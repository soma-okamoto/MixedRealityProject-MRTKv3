Shader "CustomRenderTexture/PointCloudShader"
{
   Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _PointSize("Point Size", Float) = 50.0
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2g { float4 pos : POSITION; float4 color : COLOR; };
            struct g2f { float4 pos : SV_POSITION; float4 color : COLOR; };

            float _PointSize;
            fixed4 _Color;

            v2g vert(appdata v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }

            [maxvertexcount(1)]
            void geom(point v2g input[1], inout PointStream<g2f> stream)
            {
                g2f o;
                o.pos = input[0].pos;
                o.color = input[0].color;
                #if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)
                o.pos.w += 0.0001; // Avoid clipping
                #endif
                UNITY_INITIALIZE_OUTPUT(g2f, o);
                stream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG

            // 点サイズを設定
            GLSLPROGRAM
            void main() {
                gl_PointSize = _PointSize;
            }
            ENDGLSL
        }
    }
    FallBack "Diffuse"
}
