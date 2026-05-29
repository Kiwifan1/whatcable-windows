namespace WhatCable.Windows.App.Core.Localization;

/// <summary>
/// Canonical list of resource keys used by the tray app. Keeping them in one place lets the
/// ViewModels reference strongly-typed keys and lets the locale-parity test assert that every
/// <c>.resw</c> file defines the full set with no missing entries.
/// </summary>
public static class StringKeys
{
    // Tray / menu
    public const string TrayTooltip = "Tray_Tooltip";
    public const string TrayRefresh = "Tray_Refresh";
    public const string TraySettings = "Tray_Settings";
    public const string TrayQuit = "Tray_Quit";
    public const string TrayNothingConnected = "Tray_NothingConnected";
    public const string TrayNoPortsDetected = "Tray_NoPortsDetected";
    public const string TrayPortsSummary = "Tray_PortsSummary"; // "{0} of {1} ports active"

    // Port detail sections
    public const string PortActive = "Port_Active";
    public const string PortConnection = "Port_Connection";
    public const string PortConnectedDevices = "Port_ConnectedDevices";
    public const string PortTransports = "Port_Transports";
    public const string PortPdSource = "Port_PdSource";
    public const string PortCableTrustSignals = "Port_CableTrustSignals";
    public const string PortDisplay = "Port_Display";
    public const string PortThunderboltFabric = "Port_ThunderboltFabric";
    public const string PortShowTechnicalDetails = "Port_ShowTechnicalDetails";
    public const string PortUsbDevice = "Port_UsbDevice";
    public const string PortActiveCableElectronics = "Port_ActiveCableElectronics";
    public const string PortReportThisCable = "Port_ReportThisCable";
    public const string PortVideoDiagnostic = "Port_VideoDiagnostic";

    // Settings window
    public const string SettingsTitle = "Settings_Title";
    public const string SettingsBehavior = "Settings_Behavior";
    public const string SettingsLaunchAtLogin = "Settings_LaunchAtLogin";
    public const string SettingsNotifications = "Settings_Notifications";
    public const string SettingsNotifyOnCableChanges = "Settings_NotifyOnCableChanges";
    public const string SettingsHideEmptyPorts = "Settings_HideEmptyPorts";
    public const string SettingsVendorSdk = "Settings_VendorSdk";
    public const string SettingsVendorSdkDetail = "Settings_VendorSdkDetail";
    public const string SettingsLanguage = "Settings_Language";
    public const string SettingsSystemDefault = "Settings_SystemDefault";
    public const string SettingsPro = "Settings_Pro";
    public const string SettingsProLicenseKey = "Settings_ProLicenseKey";
    public const string SettingsProLicenseValid = "Settings_ProLicenseValid";
    public const string SettingsProLicenseInvalid = "Settings_ProLicenseInvalid";
    public const string SettingsPrivacy = "Settings_Privacy";
    public const string SettingsDone = "Settings_Done";

    // Notifications
    public const string NotificationChargerConnected = "Notification_ChargerConnected";
    public const string NotificationChargerDisconnected = "Notification_ChargerDisconnected";
    public const string NotificationDeviceConnected = "Notification_DeviceConnected";
    public const string NotificationDeviceDisconnected = "Notification_DeviceDisconnected";
    public const string NotificationLiquidDetectedTitle = "Notification_LiquidDetectedTitle";
    public const string NotificationLiquidDetectedBody = "Notification_LiquidDetectedBody";

    // Pro features
    public const string ProPinDiagram = "Pro_PinDiagram";
    public const string ProLiquidDetection = "Pro_LiquidDetection";
    public const string ProPowerMonitor = "Pro_PowerMonitor";
    public const string ProPowerMonitorPerPortUnavailable = "Pro_PowerMonitorPerPortUnavailable";
    public const string ProUpsell = "Pro_Upsell";
    public const string LiquidDetectionBanner = "Pro_LiquidDetectionBanner";

    // Common
    public const string CommonYes = "Common_Yes";
    public const string CommonNo = "Common_No";
    public const string CommonUnknown = "Common_Unknown";
    public const string CommonCancel = "Common_Cancel";

    /// <summary>All keys the app relies on. Used by the locale-parity test.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        TrayTooltip, TrayRefresh, TraySettings, TrayQuit, TrayNothingConnected,
        TrayNoPortsDetected, TrayPortsSummary,
        PortActive, PortConnection, PortConnectedDevices, PortTransports, PortPdSource,
        PortCableTrustSignals, PortDisplay, PortThunderboltFabric, PortShowTechnicalDetails,
        PortUsbDevice, PortActiveCableElectronics, PortReportThisCable, PortVideoDiagnostic,
        SettingsTitle, SettingsBehavior, SettingsLaunchAtLogin, SettingsNotifications,
        SettingsNotifyOnCableChanges, SettingsHideEmptyPorts, SettingsVendorSdk,
        SettingsVendorSdkDetail, SettingsLanguage, SettingsSystemDefault, SettingsPro,
        SettingsProLicenseKey, SettingsProLicenseValid, SettingsProLicenseInvalid,
        SettingsPrivacy, SettingsDone,
        NotificationChargerConnected, NotificationChargerDisconnected,
        NotificationDeviceConnected, NotificationDeviceDisconnected,
        NotificationLiquidDetectedTitle, NotificationLiquidDetectedBody,
        ProPinDiagram, ProLiquidDetection, ProPowerMonitor,
        ProPowerMonitorPerPortUnavailable, ProUpsell, LiquidDetectionBanner,
        CommonYes, CommonNo, CommonUnknown, CommonCancel,
    };
}
