using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;

namespace LayoutPlan.Script
{
    /// <summary>Displays a bounded, batched history of Unity logs.</summary>
    public class LogPrompter : SingletonMonoBehaviour<LogPrompter>
    {
        private const int MaxMessages = 30;
        private const int MaxCharacters = 6000;
        private const int MaxMessageCharacters = 1000;
        private const float RefreshIntervalSeconds = 0.2f;

        private TMP_Text _tmpText;
        public TMP_Text TmpText
        {
            get
            {
                if (_tmpText == null) _tmpText = GetComponent<TextMeshProUGUI>();
                if (_tmpText == null) _tmpText = GetComponent<TextMeshPro>();
                return _tmpText;
            }
        }

        private readonly Queue<string> _pendingLogs = new Queue<string>();
        private readonly Queue<string> _visibleLogs = new Queue<string>();
        private readonly StringBuilder _builder = new StringBuilder();

        [SerializeField, Tooltip("テキストの先頭に時刻を表示する")]
        private bool _useTimeStamp = true;

        [SerializeField, Tooltip("ログの種別に応じて色を付ける")]
        private bool _coloredByLogType = true;

        [SerializeField, Tooltip("特定の文字列を含むログは表示しない")]
        private string[] _ignorePhrases = new string[] { };

        private int _visibleCharacters;
        private float _nextRefreshTime;
        private bool _updatingText;

        private void Awake()
        {
            if (TmpText == null)
            {
                enabled = false;
                throw new NullReferenceException("No text component found.");
            }

            TmpText.overflowMode = TextOverflowModes.Overflow;
            if (_coloredByLogType) TmpText.richText = true;
            TmpText.text = string.Empty;
        }

        private void OnEnable()
        {
            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            _pendingLogs.Clear();
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logText, string stackTrace, LogType logType)
        {
            // This executes inside Debug.Log. Never update TMP or force its mesh
            // here: that stalls the caller and can generate more logs.
            if (_updatingText || !isActiveAndEnabled)
                return;
            if (_coloredByLogType && (logType == LogType.Warning || logType == LogType.Assert))
                return;
            // The original missing-glyph warning remains in the Console.
            if (logType == LogType.Warning &&
                logText.StartsWith("The character with Unicode value ", StringComparison.Ordinal))
                return;

            if (_ignorePhrases != null)
            {
                foreach (var phrase in _ignorePhrases)
                {
                    if (!string.IsNullOrEmpty(phrase) && logText.IndexOf(phrase, StringComparison.Ordinal) >= 0)
                        return;
                }
            }

            if (logText.Length > MaxMessageCharacters)
                logText = logText.Substring(0, MaxMessageCharacters) + "...";

            _builder.Clear();
            if (_useTimeStamp)
            {
                var now = DateTime.Now;
                _builder.Append($"[{now.ToLongTimeString()}:{now.Millisecond:D3}] ");
            }
            if (_coloredByLogType && (logType == LogType.Error || logType == LogType.Exception))
                _builder.Append("<color=red>").Append(logText).Append("</color>");
            else
                _builder.Append(logText);
            _builder.AppendLine();

            if (_pendingLogs.Count >= MaxMessages) _pendingLogs.Dequeue();
            _pendingLogs.Enqueue(_builder.ToString());
        }

        private void LateUpdate()
        {
            if (_pendingLogs.Count == 0 || Time.unscaledTime < _nextRefreshTime || TmpText == null)
                return;

            _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            while (_pendingLogs.Count > 0)
            {
                var entry = _pendingLogs.Dequeue();
                _visibleLogs.Enqueue(entry);
                _visibleCharacters += entry.Length;
                while (_visibleLogs.Count > MaxMessages || _visibleCharacters > MaxCharacters)
                    _visibleCharacters -= _visibleLogs.Dequeue().Length;
            }

            _builder.Clear();
            foreach (var entry in _visibleLogs) _builder.Append(entry);

            _updatingText = true;
            try
            {
                // Let Unity generate the text mesh in the normal render loop.
                TmpText.text = _builder.ToString();
            }
            finally
            {
                _updatingText = false;
            }
        }
    }
}
