using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Read-only remaining Switch List after desk Hide. Labels only (no Button).
    /// Top-right, out of the windshield. Same desk panel tint.
    /// </summary>
    public sealed class SwitchListHudOverlay : MonoBehaviour
    {
        internal static System.Action<string>? EmitLog;

        private const float Pad = 12f;
        private const float Width = 380f;
        private const float LinePx = 18f;
        private const float HeaderPx = 22f;
        private static readonly Color Panel = new Color(0.06f, 0.06f, 0.06f, 0.94f);

        private readonly string[] _lines = new string[SwitchListHudStrip.Capacity];
        private readonly GUIContent[] _contents = CreateContents();
        private GUIStyle? _style;
        private string? _boundJob;
        private int _boundIndex = int.MinValue;
        private int _boundCount = -1;
        private int _lineCount;
        private bool _wasShown;

        private static GUIContent[] CreateContents()
        {
            var arr = new GUIContent[SwitchListHudStrip.Capacity];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = new GUIContent("");
            }

            return arr;
        }

        private void OnGUI()
        {
            if (!YmsOnScreenVisibility.ShouldDraw(PlayerManager.PlayerTransform != null))
            {
                _wasShown = false;
                return;
            }

            if (!SwitchListHudStrip.ShouldDraw(
                    MapsDeskPanel.IsDeskOpen,
                    SwitchListSession.HasActive,
                    SwitchListSession.IsComplete))
            {
                _wasShown = false;
                return;
            }

            var steps = SwitchListSession.Steps;
            var index = SwitchListSession.CurrentIndex;
            var count = steps == null ? 0 : steps.Count;
            var job = SwitchListSession.JobId;
            if (job != _boundJob || index != _boundIndex || count != _boundCount)
            {
                _lineCount = SwitchListHudStrip.FillRemaining(steps, index, _lines);
                for (var i = 0; i < _lineCount; i++)
                {
                    _contents[i].text = _lines[i];
                }

                _boundJob = job;
                _boundIndex = index;
                _boundCount = count;
                if (!_wasShown)
                {
                    EmitLog?.Invoke(
                        "T2 switch-list: hud-strip from="
                        + (index + 1)
                        + " n="
                        + _lineCount);
                }
            }

            _wasShown = true;
            if (_lineCount <= 0)
            {
                return;
            }

            EnsureStyle();
            var h = HeaderPx + (_lineCount * LinePx) + 10f;
            var x = Screen.width - Pad - Width;
            var y = Pad;
            var prev = GUI.color;
            GUI.color = Panel;
            GUI.DrawTexture(new Rect(x, y, Width, h), Texture2D.whiteTexture);
            GUI.color = prev;
            GUI.Label(new Rect(x + 8, y + 2, Width - 16, HeaderPx), "Switch List", _style);
            var row = y + HeaderPx;
            for (var i = 0; i < _lineCount; i++)
            {
                GUI.Label(new Rect(x + 8, row, Width - 16, LinePx), _contents[i], _style);
                row += LinePx;
            }
        }

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            _style.normal.textColor = Color.white;
        }
    }
}
