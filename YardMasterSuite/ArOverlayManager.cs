using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Office + own-loco world markers. Fixed buffer; LateUpdate projects;
    /// OnGUI draws cached GUIContent. Pin slot stays hidden.
    /// </summary>
    public sealed class ArOverlayManager : MonoBehaviour
    {
        private const float IconPixels = 28f;
        private const float LabelWidth = 64f;
        private const float LabelHeight = 22f;
        private const float VerticalLiftMeters = 3.5f;
        private static readonly Color OfficeColor = new Color(0.25f, 0.85f, 0.35f, 0.95f);
        private static readonly Color LocoColor = new Color(0.31f, 0.76f, 0.97f, 0.95f);

        internal static Action<string>? EmitLog;

        private readonly ArMarkerSlot[] _slots = ArMarkerBuffer.Create();
        private readonly GUIContent _officeGlyph = new GUIContent("");
        private readonly GUIContent _locoGlyph = new GUIContent("");

        private GUIStyle? _style;
        private Texture2D? _officeIcon;
        private Texture2D? _locoIcon;
        private ArOverlaySnapshot? _previous;
        private bool _officeBehind;
        private ArHorizontalEdge _officeEdge = ArHorizontalEdge.None;
        private bool _locoBehind;
        private ArHorizontalEdge _locoEdge = ArHorizontalEdge.None;
        private ArPlacementHistogram _placeHist;
        private bool _wasInWorld;
        private float _lastArLogAt = -999f;
        private static ArOverlayManager? _live;

        internal static void FlushPending()
        {
            _live?.EmitPlaceSummary(force: true);
        }

        private void OnEnable()
        {
            _previous = null;
            _officeBehind = false;
            _officeEdge = ArHorizontalEdge.None;
            _locoBehind = false;
            _locoEdge = ArHorizontalEdge.None;
            _placeHist = default;
            _wasInWorld = false;
            _lastArLogAt = -999f;
            _live = this;
            StationOfficeAnchor.Clear();
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)]);
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)]);
            _officeGlyph.text = ArMarkerDisplay.Glyph(ArWaypointKind.Station);
            _locoGlyph.text = ArMarkerDisplay.Glyph(ArWaypointKind.Loco);
        }

        private void OnDisable()
        {
            EmitPlaceSummary(force: true);
            if (_live == this)
            {
                _live = null;
            }

            StationOfficeAnchor.Clear();
            DestroyIcons();
        }

        private void LateUpdate()
        {
            var inWorld = ArOverlay.ShouldDraw(PlayerManager.PlayerTransform != null);
            if (!inWorld)
            {
                HideOffice();
                HideLoco();
                if (_previous != null)
                {
                    EmitIfChanged();
                    _previous = null;
                }

                if (_wasInWorld)
                {
                    EmitPlaceSummary(force: true);
                }

                _wasInWorld = false;
                return;
            }

            _wasInWorld = true;

            var cam = PlayerManager.ActiveCamera;
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                HideOffice();
                HideLoco();
                EmitIfChanged();
                return;
            }

            UpdateOffice(cam);
            UpdateLoco(cam);
            ArEdgeStackLayout.Apply(_slots, Screen.width, Screen.height);
            ArPlacementStats.Record(_slots, Screen.height, Time.unscaledTime, ref _placeHist);
            EmitIfChanged();
            EmitPlaceSummary(force: false);
        }

        private void UpdateOffice(Camera cam)
        {
            if (!StationOfficeAnchor.TryGet(out var office, out var px, out var pz))
            {
                HideOffice();
                return;
            }

            var atOffice = ArOfficeGate.IsAtOffice(office.x, office.z, px, pz);
            if (!ArOfficeGate.ShouldShow(hasInZoneStation: true, atOffice))
            {
                HideOffice();
                return;
            }

            var world = office;
            world.y += VerticalLiftMeters;
            ProjectIntoSlot(
                cam,
                world,
                px,
                pz,
                ArWaypointKind.Station,
                ref _officeBehind,
                ref _officeEdge);
        }

        private void UpdateLoco(Camera cam)
        {
            var loco = PlayerManager.LastLoco;
            var player = PlayerManager.PlayerTransform;
            if (loco == null
                || player == null
                || !ArLocoGate.ShouldShow(
                    hasLoco: true,
                    playerIsOnThatLoco: PlayerManager.Car != null
                        && ReferenceEquals(PlayerManager.Car, loco)))
            {
                HideLoco();
                return;
            }

            var pos = player.position;
            var world = loco.transform.position;
            world.y += VerticalLiftMeters;
            ProjectIntoSlot(
                cam,
                world,
                pos.x,
                pos.z,
                ArWaypointKind.Loco,
                ref _locoBehind,
                ref _locoEdge);
        }

        private void ProjectIntoSlot(
            Camera cam,
            Vector3 world,
            float playerX,
            float playerZ,
            ArWaypointKind kind,
            ref bool wasBehind,
            ref ArHorizontalEdge edge)
        {
            var screen = cam.WorldToScreenPoint(world);
            var local = cam.transform.InverseTransformPoint(world);
            var pixelRect = cam.pixelRect;
            var previousPlace = _slots[ArMarkerBuffer.SlotOf(kind)].Place;
            ArMarkerPlacement.Resolve(
                local.z,
                local.x,
                screen.x + pixelRect.x,
                screen.y + pixelRect.y,
                screen.z,
                Screen.width,
                Screen.height,
                wasBehind,
                edge,
                previousPlace,
                out wasBehind,
                out var place,
                out var guiX,
                out var guiY,
                out edge);

            var dx = world.x - playerX;
            var dz = world.z - playerZ;
            var meters = (int)Mathf.Round(Mathf.Sqrt((dx * dx) + (dz * dz)));
            var sortKey = ArEdgeStackLayout.OutwardSortKey(
                edge,
                ArEdgeHysteresis.BehindBearingRadians(local.x, local.z));
            if (place == ArMarkerPlace.OnObject && HudStackLayout.LastBottomGuiY > 1f)
            {
                var stickyTop = ArStickyRowPlacement.StickyRowTopGuiY(HudStackLayout.LastBottomGuiY);
                guiY = stickyTop;
            }

            ArMarkerBuffer.Show(
                ref _slots[ArMarkerBuffer.SlotOf(kind)],
                kind,
                guiX,
                guiY,
                place,
                meters,
                edge,
                sortKey);
        }

        private void OnGUI()
        {
            if (!ArOverlay.ShouldDraw(PlayerManager.PlayerTransform != null))
            {
                return;
            }

            EnsureStyle();
            DrawSlot(ArWaypointKind.Station, _officeIcon, _officeGlyph);
            DrawSlot(ArWaypointKind.Loco, _locoIcon, _locoGlyph);
        }

        private void DrawSlot(ArWaypointKind kind, Texture2D? icon, GUIContent glyph)
        {
            var slot = _slots[ArMarkerBuffer.SlotOf(kind)];
            if (!ArMarkerBuffer.ShouldDrawSlot(in slot) || icon == null)
            {
                return;
            }

            var iconRect = new Rect(
                slot.GuiX - IconPixels * 0.5f,
                slot.GuiY - IconPixels,
                IconPixels,
                IconPixels);
            var labelRect = new Rect(
                slot.GuiX - LabelWidth * 0.5f,
                slot.GuiY,
                LabelWidth,
                LabelHeight);
            GUI.DrawTexture(iconRect, icon);
            GUI.Label(labelRect, glyph, _style);
        }

        private void HideOffice()
        {
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)]);
            _officeBehind = false;
            _officeEdge = ArHorizontalEdge.None;
        }

        private void HideLoco()
        {
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)]);
            _locoBehind = false;
            _locoEdge = ArHorizontalEdge.None;
        }

        private void EmitIfChanged()
        {
            var snap = ArMarkerBuffer.Snapshot(_slots);
            var line = ArTelemetry.NextLog(_previous, in snap, Time.unscaledTime, ref _lastArLogAt);
            _previous = snap;
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private void EmitPlaceSummary(bool force)
        {
            var line = ArPlacementStats.MaybeSummary(Time.unscaledTime, force, ref _placeHist);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            _officeIcon = MakeSwatch(OfficeColor);
            _locoIcon = MakeSwatch(LocoColor);
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
            };
            _style.normal.textColor = Color.white;
        }

        private static Texture2D MakeSwatch(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void DestroyIcons()
        {
            if (_officeIcon != null)
            {
                Destroy(_officeIcon);
                _officeIcon = null;
            }

            if (_locoIcon != null)
            {
                Destroy(_locoIcon);
                _locoIcon = null;
            }

            _style = null;
        }
    }
}
