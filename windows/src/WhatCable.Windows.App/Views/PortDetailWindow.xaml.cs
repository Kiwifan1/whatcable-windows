using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Text;
using Windows.Storage.Streams;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.ViewModels;

namespace WhatCable.Windows.App.Views;

/// <summary>
/// Read-only detail view for a single port, opened from the tray popover. Mirrors the macOS
/// popover's expanded sections: connected USB devices, PD power profiles, cable e-marker trust
/// signals, and the cable summary. Section headers are localised; data is bound to
/// <see cref="PortViewModel"/>.
/// </summary>
public sealed partial class PortDetailWindow : Window
{
    private readonly PortViewModel _port;
    private readonly ILocalizer _localizer;

    public PortDetailWindow(PortViewModel port, ILocalizer localizer)
    {
        _port = port ?? throw new ArgumentNullException(nameof(port));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

        InitializeComponent();

        Title = _port.Name;
        Root.DataContext = _port;
        ApplyLocalization();
        _ = RenderPinDiagramAsync();
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

    private void ApplyLocalization()
    {
        DevicesHeader.Text = _localizer.Get(StringKeys.PortConnectedDevices);
        PdHeader.Text = _localizer.Get(StringKeys.PortPdSource);
        TrustHeader.Text = _localizer.Get(StringKeys.PortCableTrustSignals);
        CableHeader.Text = _localizer.Get(StringKeys.PortActiveCableElectronics);
        PinDiagramHeader.Text = _localizer.Get(StringKeys.ProPinDiagram);
        PinDiagramUpsellBar.Message = _localizer.Get(StringKeys.ProUpsell);

        // Unavailable reason comes from the backend as a machine-readable key
        // (e.g. "ucsi_not_supported"). Look up a human-readable translation.
        if (_port.IsUnavailable && _port.UnavailableReason is not null)
        {
            UnavailableBar.Message = _localizer.Get($"Port_Unavailable_{_port.UnavailableReason}");
        }
    }

    private async System.Threading.Tasks.Task RenderPinDiagramAsync()
    {
        var svg = _port.PinDiagramSvg;
        if (!_port.ShowPinDiagram || string.IsNullOrEmpty(svg))
        {
            return;
        }

        try
        {
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(Encoding.UTF8.GetBytes(svg));
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }

            stream.Seek(0);
            var source = new SvgImageSource();
            await source.SetSourceAsync(stream);
            PinDiagramImage.Source = source;
        }
        catch (Exception)
        {
            // Rendering the diagram is best-effort; the textual signal summary remains visible.
            PinDiagramImage.Visibility = Visibility.Collapsed;
        }
    }
}
