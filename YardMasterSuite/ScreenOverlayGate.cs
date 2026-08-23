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
        private const float HandleRetrySeconds = 2f;

        private static FieldInfo? _tutorialNotificationField;
        private static PopupManager? _popups;
        private static RectTransform? _notificationRoot;
        private static float _nextHandleAt;

        public static void InvalidateHandles()
        {
            _popups = null;
            _notificationRoot = null;
            _nextHandleAt = 0f;
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

        private static void EnsureHandles()
        {
            if ((_popups != null && _notificationRoot != null)
                || Time.unscaledTime < _nextHandleAt)
            {
                return;
            }

            _nextHandleAt = Time.unscaledTime + HandleRetrySeconds;
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
