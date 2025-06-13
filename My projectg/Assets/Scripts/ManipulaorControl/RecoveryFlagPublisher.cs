using UnityEngine;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;
using System.Diagnostics;

namespace RosSharp.RosBridgeClient
{
    /// <summary>
    /// Publishes a std_msgs/Bool recovery_flag topic continuously with the current state.
    /// ToggleRecoveryFlag() を呼ぶと、true/false が切り替わりその状態を配信し続けます。
    /// </summary>
    public class RecoveryFlagPublisher : UnityPublisher<std_msgs.Bool>
    {
        private std_msgs.Bool message;
        [SerializeField]  // Inspector に表示されるようにする
        private bool currentState = false;

        /// <summary>
        /// 初期化: RosConnector で Advertise 後、初期 state(false) を設定
        /// </summary>
        protected override void Start()
        {
            base.Start();                  // RosConnector による Advertise
            message = new std_msgs.Bool();
            message.data = currentState;  // 初期は false
        }

        /// <summary>
        /// 毎 FixedUpdate フレームで最新の message.data を Publish
        /// </summary>
        private void FixedUpdate()
        {
            Publish(message);
        }

        /// <summary>
        /// recovery_flag の状態をトグルし、新状態を継続配信します
        /// </summary>
        public void ToggleRecoveryFlag()
        {
            currentState = !currentState;
            message.data = currentState;
            UnityEngine.Debug.Log($"Recovery flag state changed to {currentState}");
        }
    }
}
