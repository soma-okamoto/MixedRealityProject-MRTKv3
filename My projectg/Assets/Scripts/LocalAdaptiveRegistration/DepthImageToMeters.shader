Shader "Hidden/Appletea/DepthImageToMeters"
{
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                // The environment depth texture is a Texture2DArray.
                // Slice 0 is used consistently with the existing depth monitor.
                const float inputDepthEye = UNITY_SAMPLE_TEX2DARRAY(
                    _EnvironmentDepthTexture,
                    float3(i.uv, 0)).r;

                // Convert the normalized device depth to camera-space Z in meters.
                const float inputDepthNdc = inputDepthEye * 2.0f - 1.0f;
                const float linearDepth =
                    (1.0f / (inputDepthNdc + _EnvironmentDepthZBufferParams.y)) *
                    _EnvironmentDepthZBufferParams.x;

                // Zero is the invalid-depth value expected by ROS depth consumers.
                if (inputDepthEye <= 0.0f || inputDepthEye >= 1.0f || linearDepth <= 0.0f)
                    return 0.0f;

                return linearDepth;
            }
            ENDCG
        }
    }
}
