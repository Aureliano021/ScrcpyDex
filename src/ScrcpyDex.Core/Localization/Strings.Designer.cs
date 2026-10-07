// Copyright 2026 Aureliano Peixoto and ScrcpyDeX Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#nullable enable
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace ScrcpyDex.Core.Localization
{
    /// <summary>
    /// Strongly-typed resource class for looking up localized strings.
    /// Provides strongly-typed accessors delegating to <see cref="LocalizationManager"/>
    /// for seamless runtime culture switching.
    /// </summary>
    public static class Strings
    {
        private static ResourceManager? _resourceManager;

        /// <summary>
        /// Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    _resourceManager = new ResourceManager("ScrcpyDex.Core.Localization.Strings", typeof(Strings).Assembly);
                }
                return _resourceManager;
            }
        }

        /// <summary>
        /// Gets or sets the culture used by the localization manager.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static CultureInfo Culture
        {
            get => LocalizationManager.Instance.CurrentCulture;
            set
            {
                if (value != null)
                {
                    LocalizationManager.Instance.SetCulture(value.Name);
                }
            }
        }

        // App Branding & Titles
        public static string App_Title => LocalizationManager.Instance.GetString(nameof(App_Title));
        public static string App_Subtitle => LocalizationManager.Instance.GetString(nameof(App_Subtitle));
        public static string App_Description => LocalizationManager.Instance.GetString(nameof(App_Description));
        public static string App_VersionBadge => LocalizationManager.Instance.GetString(nameof(App_VersionBadge));
        public static string App_AutoSaved => LocalizationManager.Instance.GetString(nameof(App_AutoSaved));
        public static string App_About => LocalizationManager.Instance.GetString(nameof(App_About));
        public static string App_LegalNotices => LocalizationManager.Instance.GetString(nameof(App_LegalNotices));
        public static string App_NonAffiliationNotice => LocalizationManager.Instance.GetString(nameof(App_NonAffiliationNotice));

        // Navigation Tabs
        public static string Tab_Display => LocalizationManager.Instance.GetString(nameof(Tab_Display));
        public static string Tab_AudioInput => LocalizationManager.Instance.GetString(nameof(Tab_AudioInput));
        public static string Tab_DeviceSystem => LocalizationManager.Instance.GetString(nameof(Tab_DeviceSystem));
        public static string Tab_Logs => LocalizationManager.Instance.GetString(nameof(Tab_Logs));

        // Device Selection & State
        public static string DeviceSelector_Title => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Title));
        public static string DeviceSelector_Refresh => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Refresh));
        public static string DeviceSelector_NoDevices => LocalizationManager.Instance.GetString(nameof(DeviceSelector_NoDevices));
        public static string DeviceSelector_Unauthorized => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Unauthorized));
        public static string DeviceSelector_Connected => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Connected));
        public static string DeviceSelector_Connecting => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Connecting));
        public static string DeviceSelector_Disconnected => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Disconnected));
        public static string DeviceSelector_Details => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Details));
        public static string DeviceSelector_SurfaceId => LocalizationManager.Instance.GetString(nameof(DeviceSelector_SurfaceId));
        public static string DeviceSelector_SurfacePending => LocalizationManager.Instance.GetString(nameof(DeviceSelector_SurfacePending));
        public static string DeviceSelector_AllocatedVia => LocalizationManager.Instance.GetString(nameof(DeviceSelector_AllocatedVia));

        // Display Settings
        public static string Display_Header => LocalizationManager.Instance.GetString(nameof(Display_Header));
        public static string Display_Resolution => LocalizationManager.Instance.GetString(nameof(Display_Resolution));
        public static string Display_Resolution_Desc => LocalizationManager.Instance.GetString(nameof(Display_Resolution_Desc));
        public static string Display_Fps => LocalizationManager.Instance.GetString(nameof(Display_Fps));
        public static string Display_Fps_Desc => LocalizationManager.Instance.GetString(nameof(Display_Fps_Desc));
        public static string Display_Bitrate => LocalizationManager.Instance.GetString(nameof(Display_Bitrate));
        public static string Display_Bitrate_Desc => LocalizationManager.Instance.GetString(nameof(Display_Bitrate_Desc));
        public static string Display_Codec => LocalizationManager.Instance.GetString(nameof(Display_Codec));
        public static string Display_Codec_Desc => LocalizationManager.Instance.GetString(nameof(Display_Codec_Desc));
        public static string Display_WindowMode => LocalizationManager.Instance.GetString(nameof(Display_WindowMode));
        public static string Display_WindowMode_Desc => LocalizationManager.Instance.GetString(nameof(Display_WindowMode_Desc));

        // Audio Settings
        public static string Audio_Header => LocalizationManager.Instance.GetString(nameof(Audio_Header));
        public static string Audio_Enabled => LocalizationManager.Instance.GetString(nameof(Audio_Enabled));
        public static string Audio_Enabled_Desc => LocalizationManager.Instance.GetString(nameof(Audio_Enabled_Desc));
        public static string Audio_Buffer => LocalizationManager.Instance.GetString(nameof(Audio_Buffer));
        public static string Audio_Buffer_Desc => LocalizationManager.Instance.GetString(nameof(Audio_Buffer_Desc));
        public static string Audio_Buffer_Format => LocalizationManager.Instance.GetString(nameof(Audio_Buffer_Format));

        // Input Settings
        public static string Input_Header => LocalizationManager.Instance.GetString(nameof(Input_Header));
        public static string Input_MouseUhid => LocalizationManager.Instance.GetString(nameof(Input_MouseUhid));
        public static string Input_MouseUhid_Desc => LocalizationManager.Instance.GetString(nameof(Input_MouseUhid_Desc));
        public static string Input_ReleaseKey => LocalizationManager.Instance.GetString(nameof(Input_ReleaseKey));
        public static string Input_ReleaseKey_Desc => LocalizationManager.Instance.GetString(nameof(Input_ReleaseKey_Desc));
        public static string Input_ForwardGamepad => LocalizationManager.Instance.GetString(nameof(Input_ForwardGamepad));
        public static string Input_ForwardGamepad_Desc => LocalizationManager.Instance.GetString(nameof(Input_ForwardGamepad_Desc));
        public static string Input_StayAwake => LocalizationManager.Instance.GetString(nameof(Input_StayAwake));
        public static string Input_StayAwake_Desc => LocalizationManager.Instance.GetString(nameof(Input_StayAwake_Desc));
        public static string Input_TurnScreenOff => LocalizationManager.Instance.GetString(nameof(Input_TurnScreenOff));
        public static string Input_TurnScreenOff_Desc => LocalizationManager.Instance.GetString(nameof(Input_TurnScreenOff_Desc));
        public static string Input_Fullscreen => LocalizationManager.Instance.GetString(nameof(Input_Fullscreen));
        public static string Input_Fullscreen_Desc => LocalizationManager.Instance.GetString(nameof(Input_Fullscreen_Desc));

        // Device & Paths Settings
        public static string Device_Header => LocalizationManager.Instance.GetString(nameof(Device_Header));
        public static string Device_AutoConnect => LocalizationManager.Instance.GetString(nameof(Device_AutoConnect));
        public static string Device_AutoConnect_Desc => LocalizationManager.Instance.GetString(nameof(Device_AutoConnect_Desc));
        public static string Paths_Header => LocalizationManager.Instance.GetString(nameof(Paths_Header));
        public static string Paths_Scrcpy => LocalizationManager.Instance.GetString(nameof(Paths_Scrcpy));
        public static string Paths_Scrcpy_Desc => LocalizationManager.Instance.GetString(nameof(Paths_Scrcpy_Desc));
        public static string Paths_Scrcpy_Placeholder => LocalizationManager.Instance.GetString(nameof(Paths_Scrcpy_Placeholder));
        public static string Paths_Scrcpy_Resolved => LocalizationManager.Instance.GetString(nameof(Paths_Scrcpy_Resolved));
        public static string Paths_Scrcpy_NotFound => LocalizationManager.Instance.GetString(nameof(Paths_Scrcpy_NotFound));
        public static string Paths_ResetDefault => LocalizationManager.Instance.GetString(nameof(Paths_ResetDefault));
        public static string Paths_ResetDefault_Desc => LocalizationManager.Instance.GetString(nameof(Paths_ResetDefault_Desc));

        // Actions
        public static string Action_StartSession => LocalizationManager.Instance.GetString(nameof(Action_StartSession));
        public static string Action_StopSession => LocalizationManager.Instance.GetString(nameof(Action_StopSession));
        public static string Action_EmergencyKill => LocalizationManager.Instance.GetString(nameof(Action_EmergencyKill));
        public static string Action_Browse => LocalizationManager.Instance.GetString(nameof(Action_Browse));
        public static string Action_Refresh => LocalizationManager.Instance.GetString(nameof(Action_Refresh));
        public static string Action_Reset => LocalizationManager.Instance.GetString(nameof(Action_Reset));
        public static string Action_ResetDefaults => LocalizationManager.Instance.GetString(nameof(Action_ResetDefaults));
        public static string Action_Copy => LocalizationManager.Instance.GetString(nameof(Action_Copy));
        public static string Action_Clear => LocalizationManager.Instance.GetString(nameof(Action_Clear));
        public static string Action_ViewLogs => LocalizationManager.Instance.GetString(nameof(Action_ViewLogs));
        public static string Action_ConfigurePath => LocalizationManager.Instance.GetString(nameof(Action_ConfigurePath));

        // Status Messages
        public static string Status_Idle => LocalizationManager.Instance.GetString(nameof(Status_Idle));
        public static string Status_Handshaking => LocalizationManager.Instance.GetString(nameof(Status_Handshaking));
        public static string Status_Connecting => LocalizationManager.Instance.GetString(nameof(Status_Connecting));
        public static string Status_Connected => LocalizationManager.Instance.GetString(nameof(Status_Connected));
        public static string Status_DeployingServer => LocalizationManager.Instance.GetString(nameof(Status_DeployingServer));
        public static string Status_Streaming => LocalizationManager.Instance.GetString(nameof(Status_Streaming));
        public static string Status_Stopping => LocalizationManager.Instance.GetString(nameof(Status_Stopping));
        public static string Status_Error => LocalizationManager.Instance.GetString(nameof(Status_Error));
        public static string Status_ScrcpyMissing => LocalizationManager.Instance.GetString(nameof(Status_ScrcpyMissing));
        public static string Status_AdbUnauthorized => LocalizationManager.Instance.GetString(nameof(Status_AdbUnauthorized));

        // Diagnostic & Doctor Messages
        public static string Doctor_Title => LocalizationManager.Instance.GetString(nameof(Doctor_Title));
        public static string Doctor_AdbFound => LocalizationManager.Instance.GetString(nameof(Doctor_AdbFound));
        public static string Doctor_AdbMissing => LocalizationManager.Instance.GetString(nameof(Doctor_AdbMissing));
        public static string Doctor_ScrcpyFound => LocalizationManager.Instance.GetString(nameof(Doctor_ScrcpyFound));
        public static string Doctor_ScrcpyMissing => LocalizationManager.Instance.GetString(nameof(Doctor_ScrcpyMissing));
        public static string Doctor_UdevOk => LocalizationManager.Instance.GetString(nameof(Doctor_UdevOk));
        public static string Doctor_UdevMissing => LocalizationManager.Instance.GetString(nameof(Doctor_UdevMissing));
        public static string Doctor_VaapiOk => LocalizationManager.Instance.GetString(nameof(Doctor_VaapiOk));
        public static string Doctor_VaapiMissing => LocalizationManager.Instance.GetString(nameof(Doctor_VaapiMissing));
        public static string Doctor_UhidOk => LocalizationManager.Instance.GetString(nameof(Doctor_UhidOk));
        public static string Doctor_UhidMissing => LocalizationManager.Instance.GetString(nameof(Doctor_UhidMissing));
        public static string Doctor_DisplayServerX11 => LocalizationManager.Instance.GetString(nameof(Doctor_DisplayServerX11));
        public static string Doctor_DisplayServerWayland => LocalizationManager.Instance.GetString(nameof(Doctor_DisplayServerWayland));

        // Logs & Filters
        public static string Logs_Title => LocalizationManager.Instance.GetString(nameof(Logs_Title));
        public static string Logs_FilterPlaceholder => LocalizationManager.Instance.GetString(nameof(Logs_FilterPlaceholder));
        public static string Logs_CopyTooltip => LocalizationManager.Instance.GetString(nameof(Logs_CopyTooltip));
        public static string Logs_ClearTooltip => LocalizationManager.Instance.GetString(nameof(Logs_ClearTooltip));

        // General
        public static string General_Language => LocalizationManager.Instance.GetString(nameof(General_Language));
        public static string General_Language_Desc => LocalizationManager.Instance.GetString(nameof(General_Language_Desc));
    }
}
