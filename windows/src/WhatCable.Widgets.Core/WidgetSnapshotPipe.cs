using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace WhatCable.Widgets.Core;

/// <summary>
/// The app → widget-provider IPC channel. The WinUI app calls <see cref="PublishAsync"/> after each
/// backend poll; the provider runs a <see cref="WidgetSnapshotPipeServer"/> and re-renders its
/// widgets whenever a payload arrives. Messages are single-line UTF-8 JSON (see
/// <see cref="WidgetPayload.ToJson"/>) delimited by a newline, so a connection can be drained with a
/// single <see cref="StreamReader.ReadLine"/>.
/// </summary>
public static class WidgetSnapshotPipe
{
    internal const string LocalServer = ".";

    /// <summary>
    /// Connects to the provider's pipe and publishes a single payload. Best-effort: if the provider
    /// is not running (no widgets pinned) the connection times out and the method returns without
    /// throwing, so a failed publish never disrupts the app's poll loop.
    /// </summary>
    /// <returns><see langword="true"/> if the payload was written, <see langword="false"/> if the
    /// provider was not listening within <paramref name="connectTimeoutMs"/>.</returns>
    public static async Task<bool> PublishAsync(
        WidgetPayload payload,
        int connectTimeoutMs = 500,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        using var client = new NamedPipeClientStream(
            LocalServer,
            WidgetCatalog.SnapshotPipeName,
            PipeDirection.Out,
            PipeOptions.Asynchronous);

        try
        {
            await client.ConnectAsync(connectTimeoutMs, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return false;
        }

        var line = payload.ToJson() + "\n";
        var bytes = Encoding.UTF8.GetBytes(line);
        await client.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await client.FlushAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

/// <summary>
/// Listens on the WhatCable snapshot pipe and raises <see cref="PayloadReceived"/> for every payload
/// the app publishes. Each publisher connection carries exactly one payload. Owned by the widget
/// provider process; dispose to stop listening.
/// </summary>
public sealed class WidgetSnapshotPipeServer : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    /// <summary>Raised on a background thread for each payload received from the app.</summary>
    public event EventHandler<WidgetPayload>? PayloadReceived;

    /// <summary>Starts the accept loop. Safe to call once.</summary>
    public void Start()
    {
        _loop ??= Task.Run(() => RunAsync(_cts.Token));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    WidgetCatalog.SnapshotPipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Dispatch(line);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                // Broken pipe / client vanished mid-write: drop this connection and keep listening.
            }
        }
    }

    private void Dispatch(string line)
    {
        WidgetPayload payload;
        try
        {
            payload = WidgetPayload.FromJson(line);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // Ignore malformed payloads rather than tearing down the listener.
            return;
        }

        PayloadReceived?.Invoke(this, payload);
    }

    public void Dispose()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        _cts.Dispose();
    }
}
