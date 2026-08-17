using UnityEngine;

using YardMasterSuite.Core;



namespace YardMasterSuite

{

    /// <summary>

    /// In-world IMGUI shell: v1 centered four-bar stack (loco → look-at → job → always-on).

    /// Labels commit only on Type A events. OnGUI draws cached GUIContent.

    /// </summary>

    public sealed class HudManager : MonoBehaviour

    {

        private static readonly Color BarBackground = new Color(0.12f, 0.12f, 0.12f, 0.82f);



        private readonly GuiContentCache _labels = new GuiContentCache(HudShell.SlotCount);

        private readonly GUIContent _locoContent = new GUIContent("");

        private readonly GUIContent _lookAtContent = new GUIContent("");

        private readonly GUIContent _jobContent = new GUIContent("");

        private readonly GUIContent _alwaysOnContent = new GUIContent("");



        private bool _hasUsableLocoTrain;

        private int _headingIndex = HeadingDisplay.UnknownIndex;

        private string _alwaysOnExtras = string.Empty;

        private string _lookAtLine = string.Empty;

        private string _jobLine = string.Empty;



        private int _cars;

        private float _tonnes;

        private bool _hasCab;

        private float _reverser01;

        private float _throttlePct;

        private float _indyPct;

        private float _trainBrakePct;

        private bool _hasSpeed;

        private int _speedKmh;

        private float? _limitKmh;



        private float? _fuelPct;

        private float? _oilPct;

        private float? _gadgetMassTonnes;

        private float? _gradePct;

        private float? _loadPct;

        private string _motorsLabel = string.Empty;

        private int? _handbrakeTotal;

        private string _muLabel = string.Empty;



        private GUIStyle? _style;

        private Texture2D? _bg;

        private float _locoBarY;

        private float _lookAtBarY;

        private float _jobBarY;

        private float _alwaysOnBarY;



        private void OnEnable()

        {

            _hasUsableLocoTrain = false;

            _headingIndex = HeadingDisplay.UnknownIndex;

            _hasCab = false;

            _hasSpeed = false;

            _limitKmh = null;

            CommitAlwaysOn();

            CommitLocoBar();

            CommitLookAtBar();

            CommitJobBar();



            YmsEventBus.OnUsableTrainChanged += OnUsableTrain;

            YmsEventBus.OnPlayerBoardedTrain += OnLocoPresence;

            YmsEventBus.OnCabControlsChanged += OnCabControls;

            YmsEventBus.OnConsistChanged += OnConsist;

            YmsEventBus.OnHeadingChanged += OnHeading;

            YmsEventBus.OnSpeedChanged += OnSpeed;

            YmsEventBus.OnSpeedLimitChanged += OnLimit;

            YmsEventBus.OnLookAtBarChanged += OnLookAtBar;

            YmsEventBus.OnJobBarChanged += OnJobBar;

            YmsEventBus.OnAlwaysOnExtrasChanged += OnAlwaysOnExtras;

            YmsEventBus.OnTrainGadgetsChanged += OnTrainGadgets;

        }



        private void OnDisable()

        {

            YmsEventBus.OnUsableTrainChanged -= OnUsableTrain;

            YmsEventBus.OnPlayerBoardedTrain -= OnLocoPresence;

            YmsEventBus.OnCabControlsChanged -= OnCabControls;

            YmsEventBus.OnConsistChanged -= OnConsist;

            YmsEventBus.OnHeadingChanged -= OnHeading;

            YmsEventBus.OnSpeedChanged -= OnSpeed;

            YmsEventBus.OnSpeedLimitChanged -= OnLimit;

            YmsEventBus.OnLookAtBarChanged -= OnLookAtBar;

            YmsEventBus.OnJobBarChanged -= OnJobBar;

            YmsEventBus.OnAlwaysOnExtrasChanged -= OnAlwaysOnExtras;

            YmsEventBus.OnTrainGadgetsChanged -= OnTrainGadgets;

            HudStackLayout.Reset();

            DestroyStyles();

        }



        private void OnUsableTrain(UsableTrainState state)

        {

            _hasUsableLocoTrain = state.HasUsableLocoTrain;

            if (!_hasUsableLocoTrain)

            {

                _hasCab = false;

                _hasSpeed = false;

                _limitKmh = null;

                _cars = 0;

                _tonnes = 0;

                _gadgetMassTonnes = null;

            }



            CommitLocoBar();

        }



        private void OnLocoPresence(LocoPresence presence)

        {

            if (presence.IsBoarded)

            {

                return;

            }



            _hasCab = false;

            _hasSpeed = false;

            _limitKmh = null;

            _cars = 0;

            _tonnes = 0;

            _gadgetMassTonnes = null;

            CommitLocoBar();

        }



        private void OnCabControls(CabControlsState state)

        {

            _hasCab = true;

            _reverser01 = state.Reverser;

            _throttlePct = CabLeverDisplay.PercentFromNormalized(state.Throttle) ?? 0f;

            _indyPct = CabLeverDisplay.PercentFromNormalized(state.IndyBrake) ?? 0f;

            _trainBrakePct = CabLeverDisplay.PercentFromNormalized(state.TrainBrake) ?? 0f;

            CommitLocoBar();

        }



        private void OnConsist(ConsistSnapshot snapshot)

        {

            _cars = snapshot.CarCount;

            _tonnes = snapshot.MassTonnes;

            CommitLocoBar();

        }



        private void OnHeading(CompassHeading heading)

        {

            _headingIndex = heading.PointIndex;

            CommitAlwaysOn();

        }



        private void OnSpeed(SpeedSnapshot snapshot)

        {

            _hasSpeed = true;

            _speedKmh = snapshot.Kmh;

            CommitLocoBar();

        }



        private void OnLimit(SpeedLimitSnapshot snapshot)

        {

            _limitKmh = snapshot.LimitKmh;

            CommitLocoBar();

        }



        private void OnLookAtBar(HudBarSnapshot snapshot)

        {

            _lookAtLine = snapshot.Visible ? snapshot.Text : string.Empty;

            CommitLookAtBar();

        }



        private void OnJobBar(HudBarSnapshot snapshot)

        {

            _jobLine = snapshot.Visible ? snapshot.Text : string.Empty;

            CommitJobBar();

        }



        private void OnAlwaysOnExtras(HudBarSnapshot snapshot)

        {

            _alwaysOnExtras = snapshot.Visible ? snapshot.Text : string.Empty;

            CommitAlwaysOn();

        }



        private void OnTrainGadgets(TrainGadgetSnapshot snapshot)

        {

            _fuelPct = snapshot.FuelPercent;

            _oilPct = snapshot.OilPercent;

            _gadgetMassTonnes = snapshot.MassTonnes;

            _gradePct = snapshot.GradePercent;

            _loadPct = snapshot.LoadPercent;

            _motorsLabel = snapshot.MotorStatus ?? string.Empty;

            _handbrakeTotal = snapshot.HandbrakeApplied;

            _muLabel = snapshot.MuStatus ?? string.Empty;

            CommitLocoBar();

        }



        private void CommitAlwaysOn()

        {

            var sb = StringBuilderPool.Shared.Rent();

            HudShell.AppendAlwaysOn(sb, _headingIndex, clock: _alwaysOnExtras);

            if (_labels.TryCommit(HudShell.SlotAlwaysOnBar, sb, out var text))

            {

                _alwaysOnContent.text = text;

            }



            StringBuilderPool.Shared.Return(sb);

        }



        private void CommitLookAtBar()

        {

            if (_labels.TryCommit(HudShell.SlotLookAtBar, _lookAtLine, out var text))

            {

                _lookAtContent.text = text;

            }

        }



        private void CommitJobBar()

        {

            if (_labels.TryCommit(HudShell.SlotJobBar, _jobLine, out var text))

            {

                _jobContent.text = text;

            }

        }



        private void CommitLocoBar()

        {

            if (!HudShell.ShouldDrawLocoBar(_hasUsableLocoTrain))

            {

                if (_labels.TryCommit(HudShell.SlotLocoBar, string.Empty, out var cleared))

                {

                    _locoContent.text = cleared;

                }



                return;

            }



            var sb = StringBuilderPool.Shared.Rent();

            var speedLabel = _hasSpeed

                ? SpeedDisplay.FormatFromKmh(_speedKmh)

                : string.Empty;

            var limitLabel = _hasCab || _limitKmh != null

                ? SpeedLimitDisplay.FormatHud(

                    _hasSpeed ? _speedKmh : (float?)null,

                    _limitKmh)

                : string.Empty;



            var fuelLabel = _fuelPct is null

                ? string.Empty

                : FluidDisplay.FormatFuelHud(_fuelPct, _oilPct);

            var oilLabel = _oilPct is null

                ? string.Empty

                : FluidDisplay.FormatOilHud(_fuelPct, _oilPct);

            var gradeLabel = _gradePct is null

                ? string.Empty

                : GradeDisplay.FormatPercent(_gradePct);

            var loadLabel = _loadPct is null

                ? string.Empty

                : LoadDisplay.FormatHud(_loadPct);

            var handbrakesLabel = _handbrakeTotal is null

                ? string.Empty

                : HandbrakeDisplay.FormatTotal(_handbrakeTotal);



            HudShell.AppendLocoStopState(

                sb,

                _hasCab ? _reverser01 : (float?)null,

                _hasCab ? _throttlePct : (float?)null,

                _hasCab ? _indyPct : (float?)null,

                _hasCab ? _trainBrakePct : (float?)null,

                speedLabel,

                limitLabel,

                carCount: _cars > 0 ? _cars : (int?)null,

                massTonnes: (_gadgetMassTonnes ?? _tonnes) > 0f

                    ? (_gadgetMassTonnes ?? _tonnes)

                    : (float?)null,

                fuel: fuelLabel,

                oil: oilLabel,

                grade: gradeLabel,

                load: loadLabel,

                motors: _motorsLabel,

                handbrakes: handbrakesLabel,

                stress: _muLabel);



            if (_labels.TryCommit(HudShell.SlotLocoBar, sb, out var text))

            {

                _locoContent.text = text;

            }



            StringBuilderPool.Shared.Return(sb);

        }



        private void OnGUI()

        {

            if (!HudShell.ShouldDraw(PlayerManager.PlayerTransform != null))

            {

                HudStackLayout.Reset();

                return;

            }



            EnsureStyles();

            UpdateStackY();



            var hasLoco = HudShell.ShouldDrawLocoBar(_hasUsableLocoTrain) && !string.IsNullOrEmpty(_locoContent.text);

            var hasLookAt = !string.IsNullOrEmpty(_lookAtContent.text);

            var hasJob = !string.IsNullOrEmpty(_jobContent.text);



            if (hasLoco)

            {

                DrawCenteredBar(_locoContent, _locoBarY);

            }



            if (hasLookAt)

            {

                DrawCenteredBar(_lookAtContent, _lookAtBarY);

            }



            if (hasJob)

            {

                DrawCenteredBar(_jobContent, _jobBarY);

            }



            DrawCenteredBar(_alwaysOnContent, _alwaysOnBarY, drawWhenEmpty: true);



            HudStackLayout.PublishLastBottomGuiY(_alwaysOnBarY + MonitorHudStackLayout.BarHeight);

        }



        private void DrawCenteredBar(GUIContent label, float y, bool drawWhenEmpty = false)

        {

            if (!drawWhenEmpty && string.IsNullOrEmpty(label.text))

            {

                return;

            }



            var measure = StripRichText(label.text);

            var width = Mathf.Ceil(_style!.CalcSize(new GUIContent(measure)).x);

            var x = HudCenterLayout.CenteredBarX(width, Screen.width, MonitorHudStackLayout.Pad);

            var rect = new Rect(x, y, width, MonitorHudStackLayout.BarHeight);

            GUI.Label(rect, label, _style);

        }



        private static string StripRichText(string text)

        {

            if (string.IsNullOrEmpty(text))

            {

                return text;

            }



            var sb = StringBuilderPool.Shared.Rent();

            var inTag = false;

            foreach (var ch in text)

            {

                if (ch == '<')

                {

                    inTag = true;

                    continue;

                }



                if (ch == '>')

                {

                    inTag = false;

                    continue;

                }



                if (!inTag)

                {

                    sb.Append(ch);

                }

            }



            var plain = sb.ToString();

            StringBuilderPool.Shared.Return(sb);

            return plain;

        }



        private void UpdateStackY()

        {

            var y = MonitorHudStackLayout.Pad;

            var hasLoco = HudShell.ShouldDrawLocoBar(_hasUsableLocoTrain) && !string.IsNullOrEmpty(_locoContent.text);

            var hasLookAt = !string.IsNullOrEmpty(_lookAtContent.text);

            var hasJob = !string.IsNullOrEmpty(_jobContent.text);



            if (hasLoco)

            {

                _locoBarY = y;

                y += MonitorHudStackLayout.BarHeight + MonitorHudStackLayout.Gap;

            }



            if (hasLookAt)

            {

                _lookAtBarY = y;

                y += MonitorHudStackLayout.BarHeight + MonitorHudStackLayout.Gap;

            }



            if (hasJob)

            {

                _jobBarY = y;

                y += MonitorHudStackLayout.BarHeight + MonitorHudStackLayout.Gap;

            }



            _alwaysOnBarY = y;

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

            _style = new GUIStyle(GUI.skin.box)

            {

                fontSize = 16,

                alignment = TextAnchor.MiddleLeft,

                clipping = TextClipping.Clip,

                richText = true,

                padding = new RectOffset(10, 10, 4, 4),

                border = new RectOffset(0, 0, 0, 0),

                margin = new RectOffset(0, 0, 0, 0),

            };

            _style.normal.textColor = Color.white;

            _style.normal.background = _bg;

            _style.hover.background = _bg;

            _style.active.background = _bg;

            _style.focused.background = _bg;

            _style.onNormal.background = _bg;

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


