using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// In-world IMGUI shell: always-on compass + consist/cab top bar.
    /// Labels commit only on Type A events. OnGUI draws cached GUIContent.
    /// </summary>
    public sealed class HudManager : MonoBehaviour
    {
        private static readonly Color BarBackground = new Color(0.12f, 0.12f, 0.12f, 0.82f);

        private const float Pad = 8f;
        private const float BarHeight = 26f;
        private const float BarGap = 4f;

        private readonly GuiContentCache _labels = new GuiContentCache(HudShell.SlotCount);
        private readonly GUIContent _compassContent = new GUIContent("");
        private readonly GUIContent _topBarContent = new GUIContent("");

        private int _headingIndex = HeadingDisplay.UnknownIndex;
        private bool _hasConsist;
        private int _cars;
        private int _tonnes;
        private bool _hasCab;
        private int _thr;
        private int _indy;
        private int _train;
        private bool _engPresent;
        private int _eng;
        private int _rev;

        private GUIStyle? _style;
        private Texture2D? _bg;
        private int _screenW = -1;
        private int _screenH = -1;
        private Rect _compassRect;
        private Rect _topBarRect;

        private void OnEnable()
        {
            _headingIndex = HeadingDisplay.UnknownIndex;
            _hasConsist = false;
            _hasCab = false;
            CommitCompass();
            CommitTopBar();
            YmsEventBus.OnPlayerBoardedTrain += OnLocoPresence;
            YmsEventBus.OnCabControlsChanged += OnCabControls;
            YmsEventBus.OnConsistChanged += OnConsist;
            YmsEventBus.OnHeadingChanged += OnHeading;
        }

        private void OnDisable()
        {
            YmsEventBus.OnPlayerBoardedTrain -= OnLocoPresence;
            YmsEventBus.OnCabControlsChanged -= OnCabControls;
            YmsEventBus.OnConsistChanged -= OnConsist;
            YmsEventBus.OnHeadingChanged -= OnHeading;
            DestroyStyles();
        }

        private void OnLocoPresence(LocoPresence presence)
        {
            if (presence.IsBoarded)
            {
                return;
            }

            _hasCab = false;
            CommitTopBar();
        }

        private void OnCabControls(CabControlsState state)
        {
            _hasCab = true;
            _thr = ControlTelemetry.ToPct(state.Throttle);
            _indy = ControlTelemetry.ToPct(state.IndyBrake);
            _train = ControlTelemetry.ToPct(state.TrainBrake);
            _engPresent = state.HasEngineBrake;
            _eng = state.HasEngineBrake ? ControlTelemetry.ToPct(state.EngineBrake) : 0;
            _rev = ControlTelemetry.ToPct(state.Reverser);
            CommitTopBar();
        }

        private void OnConsist(ConsistSnapshot snapshot)
        {
            _hasConsist = true;
            _cars = snapshot.CarCount;
            _tonnes = snapshot.MassTonnes;
            CommitTopBar();
        }

        private void OnHeading(CompassHeading heading)
        {
            _headingIndex = heading.PointIndex;
            CommitCompass();
        }

        private void CommitCompass()
        {
            var sb = StringBuilderPool.Shared.Rent();
            HudShell.AppendCompass(sb, _headingIndex);
            if (_labels.TryCommit(HudShell.SlotCompass, sb, out var text))
            {
                _compassContent.text = text;
            }

            StringBuilderPool.Shared.Return(sb);
        }

        private void CommitTopBar()
        {
            var sb = StringBuilderPool.Shared.Rent();
            HudShell.AppendTopBar(
                sb,
                _hasConsist, _cars, _tonnes,
                _hasCab, _thr, _indy, _train, _engPresent, _eng, _rev);
            if (_labels.TryCommit(HudShell.SlotTopBar, sb, out var text))
            {
                _topBarContent.text = text;
            }

            StringBuilderPool.Shared.Return(sb);
        }

        private void OnGUI()
        {
            if (!HudShell.ShouldDraw(PlayerManager.PlayerTransform != null))
            {
                return;
            }

            EnsureStyles();
            EnsureRects();
            GUI.DrawTexture(_compassRect, _bg);
            GUI.Label(_compassRect, _compassContent, _style);
            if (!HudShell.ShouldDrawTopBar(_hasConsist, _hasCab))
            {
                return;
            }

            GUI.DrawTexture(_topBarRect, _bg);
            GUI.Label(_topBarRect, _topBarContent, _style);
        }

        private void EnsureRects()
        {
            var w = Screen.width;
            var h = Screen.height;
            if (w == _screenW && h == _screenH)
            {
                return;
            }

            _screenW = w;
            _screenH = h;
            var width = w - Pad * 2f;
            _compassRect = new Rect(Pad, Pad, width, BarHeight);
            _topBarRect = new Rect(Pad, Pad + BarHeight + BarGap, width, BarHeight);
        }

        private void EnsureStyles()
        {
            if (_style != null)
            {
                return;
            }

            _bg = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _bg.SetPixel(0, 0, BarBackground);
            _bg.Apply();
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            _style.normal.textColor = Color.white;
            _style.padding = new RectOffset(10, 8, 0, 0);
        }

        private void DestroyStyles()
        {
            if (_bg != null)
            {
                Destroy(_bg);
                _bg = null;
            }

            _style = null;
        }
    }
}
