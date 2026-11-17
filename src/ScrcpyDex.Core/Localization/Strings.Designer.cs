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
        public static string Tab_About => LocalizationManager.Instance.GetString(nameof(Tab_About));

        // Device Selection & State
        public static string DeviceSelector_Title => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Title));
        public static string DeviceSelector_Header => LocalizationManager.Instance.GetString(nameof(DeviceSelector_Header));
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
        public static string Action_StartMirroring => LocalizationManager.Instance.GetString(nameof(Action_StartMirroring));
        public static string Status_MirroringReady => LocalizationManager.Instance.GetString(nameof(Status_MirroringReady));
        public static string Device_NonDexNotice => LocalizationManager.Instance.GetString(nameof(Device_NonDexNotice));
        public static string Action_StopSession => LocalizationManager.Instance.GetString(nameof(Action_StopSession));
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

        // MainWindow Avalonia UI
        public static string MainWindow_Title => LocalizationManager.Instance.GetString(nameof(MainWindow_Title));
        public static string MainWindow_EngineTag => LocalizationManager.Instance.GetString(nameof(MainWindow_EngineTag));
        public static string Device_DexModeReady => LocalizationManager.Instance.GetString(nameof(Device_DexModeReady));
        public static string DeviceSelector_TargetLabel => LocalizationManager.Instance.GetString(nameof(DeviceSelector_TargetLabel));
        public static string DeviceSelector_NoDevicesPlaceholder => LocalizationManager.Instance.GetString(nameof(DeviceSelector_NoDevicesPlaceholder));
        public static string Telemetry_Header => LocalizationManager.Instance.GetString(nameof(Telemetry_Header));
        public static string Telemetry_Battery => LocalizationManager.Instance.GetString(nameof(Telemetry_Battery));
        public static string Telemetry_Temperature => LocalizationManager.Instance.GetString(nameof(Telemetry_Temperature));
        public static string Telemetry_Link => LocalizationManager.Instance.GetString(nameof(Telemetry_Link));
        public static string Telemetry_Latency => LocalizationManager.Instance.GetString(nameof(Telemetry_Latency));
        public static string DeviceSelector_RescanTooltip => LocalizationManager.Instance.GetString(nameof(DeviceSelector_RescanTooltip));
        public static string Action_Rescan => LocalizationManager.Instance.GetString(nameof(Action_Rescan));
        public static string Action_StopSessionText => LocalizationManager.Instance.GetString(nameof(Action_StopSessionText));
        public static string Action_ConfigureScrcpyTooltip => LocalizationManager.Instance.GetString(nameof(Action_ConfigureScrcpyTooltip));
        public static string Action_ConfigureScrcpy => LocalizationManager.Instance.GetString(nameof(Action_ConfigureScrcpy));
        public static string Display_CardHeader => LocalizationManager.Instance.GetString(nameof(Display_CardHeader));
        public static string Display_ResolutionLabel => LocalizationManager.Instance.GetString(nameof(Display_ResolutionLabel));
        public static string Display_TargetFrameRate => LocalizationManager.Instance.GetString(nameof(Display_TargetFrameRate));
        public static string Display_VideoCodec => LocalizationManager.Instance.GetString(nameof(Display_VideoCodec));
        public static string Display_AudioStreamDriver => LocalizationManager.Instance.GetString(nameof(Display_AudioStreamDriver));
        public static string Display_RenderingDriver => LocalizationManager.Instance.GetString(nameof(Display_RenderingDriver));
        public static string Display_RenderingDriverDesc => LocalizationManager.Instance.GetString(nameof(Display_RenderingDriverDesc));
        public static string Display_TransmissionBitrate => LocalizationManager.Instance.GetString(nameof(Display_TransmissionBitrate));
        public static string Display_BitratePresetRecommended => LocalizationManager.Instance.GetString(nameof(Display_BitratePresetRecommended));
        public static string QuickOptions_Header => LocalizationManager.Instance.GetString(nameof(QuickOptions_Header));
        public static string QuickOptions_TurnScreenOff => LocalizationManager.Instance.GetString(nameof(QuickOptions_TurnScreenOff));
        public static string QuickOptions_TurnScreenOff_Desc => LocalizationManager.Instance.GetString(nameof(QuickOptions_TurnScreenOff_Desc));
        public static string QuickOptions_NativeUhidMouse => LocalizationManager.Instance.GetString(nameof(QuickOptions_NativeUhidMouse));
        public static string QuickOptions_NativeUhidMouse_Desc => LocalizationManager.Instance.GetString(nameof(QuickOptions_NativeUhidMouse_Desc));
        public static string QuickOptions_ForwardShortcuts => LocalizationManager.Instance.GetString(nameof(QuickOptions_ForwardShortcuts));
        public static string QuickOptions_ForwardShortcuts_Desc => LocalizationManager.Instance.GetString(nameof(QuickOptions_ForwardShortcuts_Desc));
        public static string QuickOptions_StayAwake => LocalizationManager.Instance.GetString(nameof(QuickOptions_StayAwake));
        public static string QuickOptions_StayAwake_Desc => LocalizationManager.Instance.GetString(nameof(QuickOptions_StayAwake_Desc));
        public static string QuickOptions_LowLatencyAudio => LocalizationManager.Instance.GetString(nameof(QuickOptions_LowLatencyAudio));
        public static string QuickOptions_LowLatencyAudio_Desc => LocalizationManager.Instance.GetString(nameof(QuickOptions_LowLatencyAudio_Desc));
        public static string Console_Header => LocalizationManager.Instance.GetString(nameof(Console_Header));
        public static string Console_ActivePipe => LocalizationManager.Instance.GetString(nameof(Console_ActivePipe));
        public static string Console_CopyCli => LocalizationManager.Instance.GetString(nameof(Console_CopyCli));
        public static string Console_CopyLogs => LocalizationManager.Instance.GetString(nameof(Console_CopyLogs));
        public static string Footer_AdbDaemon => LocalizationManager.Instance.GetString(nameof(Footer_AdbDaemon));
        public static string Footer_EncoderFormat => LocalizationManager.Instance.GetString(nameof(Footer_EncoderFormat));
        public static string Footer_RoundtripLabel => LocalizationManager.Instance.GetString(nameof(Footer_RoundtripLabel));

        // FAQ & Troubleshooting Drawer
        public static string Faq_Title => LocalizationManager.Instance.GetString(nameof(Faq_Title));
        public static string Faq_Subtitle => LocalizationManager.Instance.GetString(nameof(Faq_Subtitle));
        public static string Faq_SearchPlaceholder => LocalizationManager.Instance.GetString(nameof(Faq_SearchPlaceholder));
        public static string Faq_Close => LocalizationManager.Instance.GetString(nameof(Faq_Close));
        public static string Faq_Category_All => LocalizationManager.Instance.GetString(nameof(Faq_Category_All));
        public static string Faq_Category_Connection => LocalizationManager.Instance.GetString(nameof(Faq_Category_Connection));
        public static string Faq_Category_DeX => LocalizationManager.Instance.GetString(nameof(Faq_Category_DeX));
        public static string Faq_Category_Display => LocalizationManager.Instance.GetString(nameof(Faq_Category_Display));
        public static string Faq_Category_Input => LocalizationManager.Instance.GetString(nameof(Faq_Category_Input));
        public static string Faq_Category_Battery => LocalizationManager.Instance.GetString(nameof(Faq_Category_Battery));
        public static string Faq_Label_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Label_Symptom));
        public static string Faq_Label_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Label_Cause));
        public static string Faq_Label_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Label_Solution));
        public static string Faq_Label_CopyCommand => LocalizationManager.Instance.GetString(nameof(Faq_Label_CopyCommand));
        public static string Faq_NoResults => LocalizationManager.Instance.GetString(nameof(Faq_NoResults));
        public static string Faq_NoResults_Desc => LocalizationManager.Instance.GetString(nameof(Faq_NoResults_Desc));

        public static string Faq_Q1_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q1_Title));
        public static string Faq_Q1_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q1_Symptom));
        public static string Faq_Q1_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q1_Cause));
        public static string Faq_Q1_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q1_Solution));

        public static string Faq_Q2_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q2_Title));
        public static string Faq_Q2_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q2_Symptom));
        public static string Faq_Q2_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q2_Cause));
        public static string Faq_Q2_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q2_Solution));

        public static string Faq_Q3_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q3_Title));
        public static string Faq_Q3_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q3_Symptom));
        public static string Faq_Q3_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q3_Cause));
        public static string Faq_Q3_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q3_Solution));

        public static string Faq_Q4_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q4_Title));
        public static string Faq_Q4_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q4_Symptom));
        public static string Faq_Q4_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q4_Cause));
        public static string Faq_Q4_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q4_Solution));

        public static string Faq_Q5_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q5_Title));
        public static string Faq_Q5_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q5_Symptom));
        public static string Faq_Q5_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q5_Cause));
        public static string Faq_Q5_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q5_Solution));

        public static string Faq_Q6_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q6_Title));
        public static string Faq_Q6_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q6_Symptom));
        public static string Faq_Q6_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q6_Cause));
        public static string Faq_Q6_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q6_Solution));

        public static string Faq_Q7_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q7_Title));
        public static string Faq_Q7_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q7_Symptom));
        public static string Faq_Q7_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q7_Cause));
        public static string Faq_Q7_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q7_Solution));

        public static string Faq_Q8_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q8_Title));
        public static string Faq_Q8_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q8_Symptom));
        public static string Faq_Q8_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q8_Cause));
        public static string Faq_Q8_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q8_Solution));

        public static string Faq_Q9_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q9_Title));
        public static string Faq_Q9_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q9_Symptom));
        public static string Faq_Q9_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q9_Cause));
        public static string Faq_Q9_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q9_Solution));

        public static string Faq_Q10_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q10_Title));
        public static string Faq_Q10_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q10_Symptom));
        public static string Faq_Q10_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q10_Cause));
        public static string Faq_Q10_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q10_Solution));

        public static string Faq_Q11_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q11_Title));
        public static string Faq_Q11_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q11_Symptom));
        public static string Faq_Q11_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q11_Cause));
        public static string Faq_Q11_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q11_Solution));

        public static string Faq_Q12_Title => LocalizationManager.Instance.GetString(nameof(Faq_Q12_Title));
        public static string Faq_Q12_Symptom => LocalizationManager.Instance.GetString(nameof(Faq_Q12_Symptom));
        public static string Faq_Q12_Cause => LocalizationManager.Instance.GetString(nameof(Faq_Q12_Cause));
        public static string Faq_Q12_Solution => LocalizationManager.Instance.GetString(nameof(Faq_Q12_Solution));

        public static string Window_Tooltip_Settings => LocalizationManager.Instance.GetString(nameof(Window_Tooltip_Settings));
        public static string Window_Tooltip_Minimize => LocalizationManager.Instance.GetString(nameof(Window_Tooltip_Minimize));
        public static string Window_Tooltip_Close => LocalizationManager.Instance.GetString(nameof(Window_Tooltip_Close));
        public static string Telemetry_UsbLink => LocalizationManager.Instance.GetString(nameof(Telemetry_UsbLink));
        public static string Ui_TargetDevice => LocalizationManager.Instance.GetString(nameof(Ui_TargetDevice));
        public static string Ui_Mirror => LocalizationManager.Instance.GetString(nameof(Ui_Mirror));
        public static string Ui_Back => LocalizationManager.Instance.GetString(nameof(Ui_Back));
        public static string Ui_Configuration => LocalizationManager.Instance.GetString(nameof(Ui_Configuration));
        public static string Ui_Console => LocalizationManager.Instance.GetString(nameof(Ui_Console));
        public static string Ui_ConsoleTooltip => LocalizationManager.Instance.GetString(nameof(Ui_ConsoleTooltip));
        public static string Ui_FaqDoctor => LocalizationManager.Instance.GetString(nameof(Ui_FaqDoctor));
        public static string Ui_Faq => LocalizationManager.Instance.GetString(nameof(Ui_Faq));
        public static string Ui_WirelessConnection => LocalizationManager.Instance.GetString(nameof(Ui_WirelessConnection));
        public static string Ui_HowToConnectQr => LocalizationManager.Instance.GetString(nameof(Ui_HowToConnectQr));
        public static string Ui_QrHelpTitle => LocalizationManager.Instance.GetString(nameof(Ui_QrHelpTitle));
        public static string Ui_QrHelpSteps => LocalizationManager.Instance.GetString(nameof(Ui_QrHelpSteps));
        public static string Ui_RegenerateQr => LocalizationManager.Instance.GetString(nameof(Ui_RegenerateQr));
        public static string Qr_WaitingForScan => LocalizationManager.Instance.GetString(nameof(Qr_WaitingForScan));
        public static string Qr_Pairing => LocalizationManager.Instance.GetString(nameof(Qr_Pairing));
        public static string Qr_Connecting => LocalizationManager.Instance.GetString(nameof(Qr_Connecting));
        public static string Qr_Failed => LocalizationManager.Instance.GetString(nameof(Qr_Failed));
        public static string Action_SwitchToWifiTooltip => LocalizationManager.Instance.GetString(nameof(Action_SwitchToWifiTooltip));
        public static string Telemetry_WirelessConnection => LocalizationManager.Instance.GetString(nameof(Telemetry_WirelessConnection));
        public static string Status_SwitchingToWifi => LocalizationManager.Instance.GetString(nameof(Status_SwitchingToWifi));
        public static string Wireless_UnplugBadge => LocalizationManager.Instance.GetString(nameof(Wireless_UnplugBadge));
        public static string Wireless_UnplugTitle => LocalizationManager.Instance.GetString(nameof(Wireless_UnplugTitle));
        public static string Wireless_UnplugDesc => LocalizationManager.Instance.GetString(nameof(Wireless_UnplugDesc));
        public static string Wireless_RevertToUsb => LocalizationManager.Instance.GetString(nameof(Wireless_RevertToUsb));
        public static string Wireless_NonTlsBadge => LocalizationManager.Instance.GetString(nameof(Wireless_NonTlsBadge));
        public static string Wireless_NonTlsTitle => LocalizationManager.Instance.GetString(nameof(Wireless_NonTlsTitle));
        public static string Wireless_NonTlsDesc => LocalizationManager.Instance.GetString(nameof(Wireless_NonTlsDesc));
        public static string Ui_Close => LocalizationManager.Instance.GetString(nameof(Ui_Close));
        public static string Ui_DisplayEncoder => LocalizationManager.Instance.GetString(nameof(Ui_DisplayEncoder));
        public static string Ui_CustomDimensions => LocalizationManager.Instance.GetString(nameof(Ui_CustomDimensions));
        public static string Ui_DisplayDensityDpi => LocalizationManager.Instance.GetString(nameof(Ui_DisplayDensityDpi));
        public static string Ui_SessionInput => LocalizationManager.Instance.GetString(nameof(Ui_SessionInput));
        public static string Ui_MouseInputDriver => LocalizationManager.Instance.GetString(nameof(Ui_MouseInputDriver));
        public static string Ui_LaunchFullscreen => LocalizationManager.Instance.GetString(nameof(Ui_LaunchFullscreen));
        public static string Ui_SystemBinaries => LocalizationManager.Instance.GetString(nameof(Ui_SystemBinaries));
        public static string Ui_ScrcpyExecutable => LocalizationManager.Instance.GetString(nameof(Ui_ScrcpyExecutable));
        public static string Ui_AdbExecutable => LocalizationManager.Instance.GetString(nameof(Ui_AdbExecutable));
        public static string Ui_AutoDetectPath => LocalizationManager.Instance.GetString(nameof(Ui_AutoDetectPath));
        public static string Ui_Browse => LocalizationManager.Instance.GetString(nameof(Ui_Browse));
        public static string Ui_Settings => LocalizationManager.Instance.GetString(nameof(Ui_Settings));
        public static string Ui_FaqDiagnostics => LocalizationManager.Instance.GetString(nameof(Ui_FaqDiagnostics));
        public static string Ui_CheckEnv => LocalizationManager.Instance.GetString(nameof(Ui_CheckEnv));
        public static string Ui_Copy => LocalizationManager.Instance.GetString(nameof(Ui_Copy));
        public static string Ui_CriticalAlert => LocalizationManager.Instance.GetString(nameof(Ui_CriticalAlert));
        public static string Ui_LocateScrcpy => LocalizationManager.Instance.GetString(nameof(Ui_LocateScrcpy));
        public static string Ui_LocateAdb => LocalizationManager.Instance.GetString(nameof(Ui_LocateAdb));
        public static string Ui_Retry => LocalizationManager.Instance.GetString(nameof(Ui_Retry));
        public static string Ui_Dismiss => LocalizationManager.Instance.GetString(nameof(Ui_Dismiss));
        public static string Console_WindowTitle => LocalizationManager.Instance.GetString(nameof(Console_WindowTitle));
        public static string Console_Subtitle => LocalizationManager.Instance.GetString(nameof(Console_Subtitle));
        public static string Error_ScrcpyNotFoundTitle => LocalizationManager.Instance.GetString(nameof(Error_ScrcpyNotFoundTitle));
        public static string Error_AdbNotFoundTitle => LocalizationManager.Instance.GetString(nameof(Error_AdbNotFoundTitle));
        public static string Error_EnvironmentTitle => LocalizationManager.Instance.GetString(nameof(Error_EnvironmentTitle));
        public static string Error_SessionExecutionTitle => LocalizationManager.Instance.GetString(nameof(Error_SessionExecutionTitle));
        public static string Error_GenericTitle => LocalizationManager.Instance.GetString(nameof(Error_GenericTitle));
        public static string Error_ScrcpyNotFoundDesc => LocalizationManager.Instance.GetString(nameof(Error_ScrcpyNotFoundDesc));
        public static string Error_AdbNotFoundDesc => LocalizationManager.Instance.GetString(nameof(Error_AdbNotFoundDesc));
        public static string StatusPill_MirrorLive => LocalizationManager.Instance.GetString(nameof(StatusPill_MirrorLive));
        public static string StatusPill_DexLive => LocalizationManager.Instance.GetString(nameof(StatusPill_DexLive));
        public static string StatusPill_Starting => LocalizationManager.Instance.GetString(nameof(StatusPill_Starting));
        public static string StatusPill_Stopping => LocalizationManager.Instance.GetString(nameof(StatusPill_Stopping));
        public static string StatusPill_Error => LocalizationManager.Instance.GetString(nameof(StatusPill_Error));
        public static string StatusPill_DexReady => LocalizationManager.Instance.GetString(nameof(StatusPill_DexReady));
        public static string StatusPill_Mirror => LocalizationManager.Instance.GetString(nameof(StatusPill_Mirror));
        public static string StatusPill_Offline => LocalizationManager.Instance.GetString(nameof(StatusPill_Offline));
        public static string Mode_NoTarget => LocalizationManager.Instance.GetString(nameof(Mode_NoTarget));
        public static string Mode_SamsungDex => LocalizationManager.Instance.GetString(nameof(Mode_SamsungDex));
        public static string Mode_ScreenMirror => LocalizationManager.Instance.GetString(nameof(Mode_ScreenMirror));
        public static string Device_NoDeviceConnected => LocalizationManager.Instance.GetString(nameof(Device_NoDeviceConnected));
        public static string Device_HardwareIdle => LocalizationManager.Instance.GetString(nameof(Device_HardwareIdle));
        public static string Status_FoundDevices => LocalizationManager.Instance.GetString(nameof(Status_FoundDevices));
        public static string Status_ScanningDevices => LocalizationManager.Instance.GetString(nameof(Status_ScanningDevices));
        public static string Status_NoDevicesDetected => LocalizationManager.Instance.GetString(nameof(Status_NoDevicesDetected));
        public static string Status_DeviceScanFailed => LocalizationManager.Instance.GetString(nameof(Status_DeviceScanFailed));
        public static string Status_NoDeviceSelected => LocalizationManager.Instance.GetString(nameof(Status_NoDeviceSelected));
        public static string Status_IdleReady => LocalizationManager.Instance.GetString(nameof(Status_IdleReady));
        public static string Status_ReadyToStart => LocalizationManager.Instance.GetString(nameof(Status_ReadyToStart));
        public static string Status_ConnectingTarget => LocalizationManager.Instance.GetString(nameof(Status_ConnectingTarget));
        public static string Status_DeployingServerMsg => LocalizationManager.Instance.GetString(nameof(Status_DeployingServerMsg));
        public static string Status_AllocatingDisplay => LocalizationManager.Instance.GetString(nameof(Status_AllocatingDisplay));
        public static string Status_StreamingActiveMsg => LocalizationManager.Instance.GetString(nameof(Status_StreamingActiveMsg));
        public static string Status_TearingDown => LocalizationManager.Instance.GetString(nameof(Status_TearingDown));
        public static string Status_SessionError => LocalizationManager.Instance.GetString(nameof(Status_SessionError));
        public static string Status_ScrcpyNotDetected => LocalizationManager.Instance.GetString(nameof(Status_ScrcpyNotDetected));
        public static string Status_AdbNotDetected => LocalizationManager.Instance.GetString(nameof(Status_AdbNotDetected));
        public static string Status_InvalidScrcpyExecutable => LocalizationManager.Instance.GetString(nameof(Status_InvalidScrcpyExecutable));
        public static string Status_InvalidAdbExecutable => LocalizationManager.Instance.GetString(nameof(Status_InvalidAdbExecutable));
        public static string Status_RendererUnsupported => LocalizationManager.Instance.GetString(nameof(Status_RendererUnsupported));
        public static string Doctor_NotRun => LocalizationManager.Instance.GetString(nameof(Doctor_NotRun));
        public static string Doctor_SummaryErrors => LocalizationManager.Instance.GetString(nameof(Doctor_SummaryErrors));
        public static string Doctor_SummaryWarnings => LocalizationManager.Instance.GetString(nameof(Doctor_SummaryWarnings));
        public static string Doctor_SummaryPassed => LocalizationManager.Instance.GetString(nameof(Doctor_SummaryPassed));
    }
}
