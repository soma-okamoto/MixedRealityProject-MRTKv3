/*
© Siemens AG, 2017-2018
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Threading;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    [RequireComponent(typeof(RosConnector))]
    public abstract class UnitySubscriber<T> : MonoBehaviour where T: Message
    {
        public string Topic;
        public float TimeStep;
        public bool EnsureThreadSafety = false;

        private RosConnector rosConnector;
        private readonly int SecondsTimeout = 1;

        protected virtual void Start()
        {
            rosConnector = GetComponent<RosConnector>();
            new Thread(Subscribe).Start();
        }

        private void Subscribe()
        {
            // RosConnector creates RosSocket on its own worker thread.  The old
            // implementation waited one second and then dereferenced RosSocket
            // even when the connection was still being established.  That race
            // could terminate this thread before the subscription was sent.
            var timeoutSeconds = rosConnector.SecondsTimeout > 0
                ? rosConnector.SecondsTimeout
                : SecondsTimeout;

            if (!rosConnector.IsConnected.WaitOne(timeoutSeconds * 1000))
            {
                Debug.LogWarning(
                    $"Failed to subscribe to '{Topic}': RosConnector was not connected " +
                    $"within {timeoutSeconds} seconds.");
                return;
            }

            if (rosConnector.RosSocket == null)
            {
                Debug.LogError($"Failed to subscribe to '{Topic}': RosSocket is null.");
                return;
            }

            rosConnector.RosSocket.Subscribe<T>(Topic, ReceiveMessage, (int)(TimeStep * 1000), ensureThreadSafety: EnsureThreadSafety); // the rate(in ms in between messages) at which to throttle the topics
            Debug.Log($"Subscribed to ROS topic '{Topic}' ({typeof(T).Name}).");
        }

        protected abstract void ReceiveMessage(T message);

    }
}
