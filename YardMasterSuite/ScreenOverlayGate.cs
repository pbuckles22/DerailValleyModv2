using System.Reflection;
using DV;
using DV.UI;
using DV.UIFramework;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Pause / save / loading / career notification / modal cover the world —
    /// AR chips must not draw on top. HUD bars may stay.
    /// </summary>
    internal static class ScreenOverlayGate
    {
        private static FieldInfo? _tutorialNotificationField;
        private static PopupManager? _popups;
        private static RectTransform? _notificationRoot;
        private static float _nextHandleAt;
        private static int _handleAttempts;

        public static void InvalidateHandles()
        {
            _popups = null;
            _notificationRoot = null;
            _nextHandleAt = 0f;
            _handleAttempts = 0;
        }

        public static bool WorldReady()
        {
            try
            {
                if (LoadingScreenManager.IsLoading)
                {
                    return false;
                }

                if (!WorldStreamingInit.IsLoaded)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool IsBlocking()
        {
            var pause = false;
            try
            {
                var app = AppUtil.Instance;
                pause = app != null && app.IsPauseMenuOpen;
            }
            catch
            {
                // treat as not pause; still check popups
            }

            EnsureHandles();

            var popup = false;
            try
            {
                popup = _popups != null && _popups.ActivePopup != null;
            }
            catch
            {
                // ignored
            }

            var notification = false;
            try
            {
                notification = (_notificationRoot != null && _notificationRoot.childCount > 0)
                    || TutorialFloatieActive();
            }
            catch
            {
                // ignored
            }

            return ScreenOverlayDecision.IsBlocking(pause, popup, notification);
        }

        /// <summary>Pause only — do not block Ctrl+Insert / Home / F8 on career notifications.</summary>
        public static bool BlocksToolHotkeys()
        {
            try
            {
                var app = AppUtil.Instance;
                return ScreenOverlayDecision.BlocksToolHotkeys(
                    app != null && app.IsPauseMenuOpen);
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureHandles()
        {
            var now = Time.unscaledTime;
            if (!ScreenOverlayHandlePolicy.ShouldLookup(
                    _popups != null,
                    _notificationRoot != null,
                    _handleAttempts,
                    now,
                    _nextHandleAt))
            {
                return;
            }

            _handleAttempts++;
            _nextHandleAt = now + ScreenOverlayHandlePolicy.RetrySeconds;
            try
            {
                if (_popups == null)
                {
                    _popups = Object.FindObjectOfType<PopupManager>();
                }

                if (_notificationRoot == null)
                {
                    var provider = Object.FindObjectOfType<NonVRNotificationManagerProvider>();
                    _notificationRoot = provider != null ? provider.ContentRoot : null;
                }
            }
            catch
            {
                // ignored
            }
        }

        private static bool TutorialFloatieActive()
        {
            var helper = TutorialHelper.Instance;
            if (helper == null)
            {
                return false;
            }

            _tutorialNotificationField ??= typeof(TutorialHelper).GetField(
                "notification",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var go = _tutorialNotificationField?.GetValue(helper) as GameObject;
            return go != null && go.activeInHierarchy;
        }
    }
}
