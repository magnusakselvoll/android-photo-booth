﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MagnusAkselvoll.AndroidPhotoBooth.Camera.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.10.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string AdbPath {
            get {
                return ((string)(this["AdbPath"]));
            }
            set {
                this["AdbPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PinCode {
            get {
                return ((string)(this["PinCode"]));
            }
            set {
                this["PinCode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("STILL_IMAGE_CAMERA")]
        public string CameraApp {
            get {
                return ((string)(this["CameraApp"]));
            }
            set {
                this["CameraApp"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00:00:15")]
        public global::System.TimeSpan FocusKeepaliveInterval {
            get {
                return ((global::System.TimeSpan)(this["FocusKeepaliveInterval"]));
            }
            set {
                this["FocusKeepaliveInterval"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("/sdcard/DCIM/Camera/")]
        public string DeviceImageFolder {
            get {
                return ((string)(this["DeviceImageFolder"]));
            }
            set {
                this["DeviceImageFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool DeleteAfterDownload {
            get {
                return ((bool)(this["DeleteAfterDownload"]));
            }
            set {
                this["DeleteAfterDownload"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string WorkingFolder {
            get {
                return ((string)(this["WorkingFolder"]));
            }
            set {
                this["WorkingFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PublishFolder {
            get {
                return ((string)(this["PublishFolder"]));
            }
            set {
                this["PublishFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("^.*\\.jpg$")]
        public string FileSelectionRegex {
            get {
                return ((string)(this["FileSelectionRegex"]));
            }
            set {
                this["FileSelectionRegex"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("{0}")]
        public string PublishFilenamePattern {
            get {
                return ((string)(this["PublishFilenamePattern"]));
            }
            set {
                this["PublishFilenamePattern"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00:00:10")]
        public global::System.TimeSpan DownloadImagesInterval {
            get {
                return ((global::System.TimeSpan)(this["DownloadImagesInterval"]));
            }
            set {
                this["DownloadImagesInterval"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int PublishFilesPerFolder {
            get {
                return ((int)(this["PublishFilesPerFolder"]));
            }
            set {
                this["PublishFilesPerFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseNfcScreenApi {
            get {
                return ((bool)(this["UseNfcScreenApi"]));
            }
            set {
                this["UseNfcScreenApi"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00:00:30")]
        public global::System.TimeSpan CameraOpenTimeout {
            get {
                return ((global::System.TimeSpan)(this["CameraOpenTimeout"]));
            }
            set {
                this["CameraOpenTimeout"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00000000-0000-0000-0000-000000000000")]
        public global::System.Guid Joystick {
            get {
                return ((global::System.Guid)(this["Joystick"]));
            }
            set {
                this["Joystick"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string JoystickButton {
            get {
                return ((string)(this["JoystickButton"]));
            }
            set {
                this["JoystickButton"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00:01:00")]
        public global::System.TimeSpan InactivityLockTimeout {
            get {
                return ((global::System.TimeSpan)(this["InactivityLockTimeout"]));
            }
            set {
                this["InactivityLockTimeout"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int CountDown {
            get {
                return ((int)(this["CountDown"]));
            }
            set {
                this["CountDown"] = value;
            }
        }
    }
}
