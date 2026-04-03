// Copyright (c) 2026 Klemens Urban <klemens.urban@outlook.com>
// SPDX-License-Identifier: GPL-3.0-only

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nocscienceat.WinWingEcam.Configuration;
using nocscienceat.WinWingEcam.Hardware;
using nocscienceat.XPlanePanel;
using nocscienceat.XPlanePanel.Services;
using nocscienceat.XPlaneWebConnector.Interfaces;

namespace nocscienceat.WinWingEcam.Panels;

/// <summary>
/// WinWing ECAM panel handler. Reads button presses and drives annunciator LEDs
/// via USB HID, bridging ECAM page selection to X-Plane commands via ToLiss.
/// </summary>
public partial class EcamPanelHandler : PanelHandlerBase<WinWingEcamConfig>
{
    private WinWingEcamHidDevice? _hidDevice;
    private bool _baselineEstablished;
    private uint _lastButtons;
    private bool _teleportRecoveryActive;

    public override string PanelName => "ECAM";
    public override bool IsConnected => _hidDevice?.IsOpen ?? false;

    public EcamPanelHandler(IXPlaneWebConnector connector, IConfiguration configuration,
        ILogger<EcamPanelHandler> logger, IDataRefCommandProvider? overrideProvider = null)
        : base(connector, configuration, logger, overrideProvider)
    {
    }

    // =====================================================================
    // Lifecycle
    // =====================================================================

    protected override async Task OnConnectedAsync(CancellationToken cancellationToken)
    {
        _hidDevice = new WinWingEcamHidDevice(_logger);

        if (!_hidDevice.FindAndOpen(_config.VendorId, _config.ProductId))
        {
            _logger.LogWarning("{Panel} no compatible WinWing ECAM device found", PanelName);
            _hidDevice.Dispose();
            _hidDevice = null;
            return;
        }

        CreateButtonDispatchTable();

        _hidDevice.ReportReceived += OnHidReport;
        _hidDevice.Disconnected += OnHidDisconnected;
        _hidDevice.StartIo(cancellationToken);

        // Startup: dim backlights, all annunciators off
        _hidDevice.SetLed(EcamLed.PanelBacklight, 110);
        _hidDevice.SetLed(EcamLed.KeyBacklight, 110);

        // Wait for ToLiss AirbusFBW plugin
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        IDisposable handshakeSub = await _connector.SubscribeAsync(
            "xplanewebconnector/AirbusFBWalive",
            (int v) => { if (v == 1) tcs.TrySetResult(v); });

        try
        {
            await _connector.SetDataRefValueAsync("xplanewebconnector/AirbusFBWalive", 1);
            await tcs.Task.WaitAsync(cancellationToken);
            _logger.LogInformation("[ECAM] AirbusFBW alive handshake verified");
        }
        finally
        {
            handshakeSub.Dispose();
        }

        await SubscribeToDataRefsAsync(cancellationToken);

        _logger.LogInformation("{Panel} connected (device: {Mask})", PanelName, _hidDevice.DeviceMask);
    }

    protected override Task OnDisconnectingAsync()
    {
        if (_hidDevice is not null)
        {
            _hidDevice.ReportReceived -= OnHidReport;
            _hidDevice.Disconnected -= OnHidDisconnected;
        }

        _hidDevice?.Dispose();
        _hidDevice = null;
        return Task.CompletedTask;
    }

    // =====================================================================
    // USB disconnect / reconnect
    // =====================================================================

    private void OnHidDisconnected()
    {
        _logger.LogWarning("[ECAM] USB device disconnected — starting reconnection polling");
        _baselineEstablished = false;

        var oldDevice = _hidDevice;
        _hidDevice = null;
        oldDevice?.Dispose();

        _ = Task.Run(async () =>
        {
            const int pollIntervalMs = 3000;
            const int maxRetries = 60;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                await Task.Delay(pollIntervalMs);
                try
                {
                    var newDevice = new WinWingEcamHidDevice(_logger);
                    if (!newDevice.FindAndOpen(_config.VendorId, _config.ProductId))
                    {
                        newDevice.Dispose();
                        continue;
                    }

                    _logger.LogInformation("[ECAM] USB device found (attempt {Attempt}) — re-initializing", attempt);

                    newDevice.ReportReceived += OnHidReport;
                    newDevice.Disconnected += OnHidDisconnected;
                    newDevice.StartIo(default);

                    _hidDevice = newDevice;
                    _baselineEstablished = false;

                    EnqueueWork(RestoreLedStates);

                    _logger.LogInformation("[ECAM] USB device re-initialized");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("[ECAM] Reconnect attempt {Attempt} failed: {Message}", attempt, ex.Message);
                }
            }

            _logger.LogError("[ECAM] Failed to reconnect after {Max} attempts — restart the application", maxRetries);
        });
    }

    // =====================================================================
    // HID report — 32-bit button bitmask
    // =====================================================================

    private void OnHidReport(byte[] data, int actualBytes)
    {
        if (!EcamConstants.ValidReportLengths.Contains(actualBytes))
        {
            _logger.LogInformation("[ECAM] HID report ignored (length={Length})", actualBytes);
            return;
        }
        if (data[0] != EcamConstants.ReportId) return;
        if (actualBytes < EcamConstants.ReportButtonOffset + EcamConstants.ReportButtonBytes) return;

        uint buttons = 0;
        for (int i = 0; i < EcamConstants.ReportButtonBytes; i++)
            buttons |= (uint)data[EcamConstants.ReportButtonOffset + i] << (8 * i);

        if (!_baselineEstablished)
        {
            _lastButtons = buttons;
            _baselineEstablished = true;
            _logger.LogInformation("[ECAM] Baseline: 0x{Buttons:X8}", buttons);
            return;
        }

        if (buttons == _lastButtons) return;

        uint changed = buttons ^ _lastButtons;
        for (int i = 0; i < EcamConstants.ButtonCount; i++)
        {
            uint mask = 1u << i;
            if ((changed & mask) == 0) continue;

            bool pressed = (buttons & mask) != 0;
            if (pressed && _buttonMap.TryGetValue(i, out var entry))
            {
                _logger.LogDebug("[ECAM] Button {Label} pressed", entry.Label);
                var cmdKey = entry.CommandKey;
                EnqueueWork(async () =>
                {
                    if (TryGetCommand(cmdKey, out var cmdPath))
                        await _connector.SendCommandAsync(cmdPath, 0.15f);
                });
            }
        }

        _lastButtons = buttons;
    }
}
