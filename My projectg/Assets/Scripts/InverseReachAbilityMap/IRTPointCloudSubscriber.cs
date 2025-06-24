/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RosSharp.RosBridgeClient
{
   public class IRTPointCloudSubscriber : RosSharp.RosBridgeClient.UnitySubscriber<RosSharp.RosBridgeClient.MessageTypes.Sensor.PointCloud2>
    {
    public Material pointMaterial;

        private ParticleSystem particleSystem;
        private List<ParticleSystem.Particle> particles = new List<ParticleSystem.Particle>();

        protected override void Start()
        {
            base.Start();
            // ParticleSystem のセットアップ
            GameObject go = new GameObject("IRMPointCloud");
            particleSystem = go.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startSize = 0.03f;
            main.maxParticles = 30000;
            main.loop = false;
            main.playOnAwake = false;
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.material = pointMaterial;
        }

        protected override void ReceiveMessage(Messages.Sensor.PointCloud2 msg)
        {
            particles.Clear();

            int height = (int)msg.height;
            int width = (int)msg.width;
            int rowStep = (int)msg.row_step;
            int pointStep = (int)msg.point_step;
            byte[] data = msg.data;

            // フィールドオフセット
            int offX = msg.fields[0].offset;  // x
            int offY = msg.fields[1].offset;  // y
            int offZ = msg.fields[2].offset;  // z
            int offScore = msg.fields[3].offset;  // score

            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    int idx = r * rowStep + c * pointStep;
                    float x = System.BitConverter.ToSingle(data, idx + offX);
                    float y = System.BitConverter.ToSingle(data, idx + offY);
                    float z = System.BitConverter.ToSingle(data, idx + offZ);
                    float score = System.BitConverter.ToSingle(data, idx + offScore);

                    // スコアを 0–1 の範囲にクランプ
                    float t = Mathf.Clamp01(score);

                    // カラーをスコアに応じて青→赤で補間
                    Color col = Color.Lerp(Color.blue, Color.red, t);

                    particles.Add(new ParticleSystem.Particle
                    {
                        position = new Vector3(x, y, z),
                        startColor = col,
                        startSize = 0.03f,
                        remainingLifetime = 1f
                    });
                }
            }
            // パーティクルを更新・再生
            particleSystem.SetParticles(particles.ToArray(), particles.Count);
            particleSystem.Play();
        }
    }


}

*/