// Copyright (c) 2026 Klemens Urban <klemens.urban@outlook.com>
// SPDX-License-Identifier: GPL-3.0-only

using Microsoft.Extensions.Logging;
using nocscienceat.WinWingEcam.Hardware;

namespace nocscienceat.WinWingEcam.Panels;

public partial class EcamPanelHandler
{
    /// <summary>Cached LED state for reconnect restore.</summary>
    private readonly Dictionary<EcamLed, int> _ledStateCache = [];

    private async Task SubscribeToDataRefsAsync(CancellationToken ct)
    {
        // ===== Brightness =====
        await SubscribeEnqueuedAsync(GetDataRefPath("PanelBrightness"), (float v) =>
        {
            int brightness = (int)(v * 255);
            SetLedCached(EcamLed.PanelBacklight, brightness);
            SetLedCached(EcamLed.KeyBacklight, brightness);
        });

        // ===== CLR L + CLR R (same dataref drives both) =====
        await SubscribeEnqueuedAsync(GetDataRefPath("CLRillum"), (int v) =>
        {
            int val = v > 0 ? 1 : 0;
            SetLedCached(EcamLed.ClrL, val);
            SetLedCached(EcamLed.ClrR, val);
        });

        // ===== SD page indicator LEDs (direct 1:1 mapping) =====
        await SubscribeLedAsync("SDENG", EcamLed.Eng);
        await SubscribeLedAsync("SDBLEED", EcamLed.Bleed);
        await SubscribeLedAsync("SDPRESS", EcamLed.Press);
        await SubscribeLedAsync("SDELEC", EcamLed.Elec);
        await SubscribeLedAsync("SDHYD", EcamLed.Hyd);
        await SubscribeLedAsync("SDFUEL", EcamLed.Fuel);
        await SubscribeLedAsync("SDAPU", EcamLed.Apu);
        await SubscribeLedAsync("SDCOND", EcamLed.Cond);
        await SubscribeLedAsync("SDDOOR", EcamLed.Door);
        await SubscribeLedAsync("SDWHEEL", EcamLed.Wheel);
        await SubscribeLedAsync("SDFCTL", EcamLed.Fctl);
        await SubscribeLedAsync("SDSTATUS", EcamLed.Sts);

        _logger.LogInformation("[ECAM] Subscribed to all datarefs");

        // ===== Cache warm-up: pre-resolve all commands =====
        var warmUpTasks = new List<Task>();
        foreach (var (_, entry) in _buttonMap)
        {
            if (TryGetCommand(entry.CommandKey, out var cmdPath))
                warmUpTasks.Add(_connector.PreResolveCommandAsync(cmdPath));
        }
        await Task.WhenAll(warmUpTasks);
        _logger.LogInformation("[ECAM] Pre-resolved {Count} command IDs", warmUpTasks.Count);

        // ===== Teleport detection =====
        await SubscribeEnqueuedAsync("xplanewebconnector/teleport", (int level) =>
        {
            _logger.LogInformation("[ECAM] Teleport detected (level={Level})", level);
            if (level >= 2 && !_teleportRecoveryActive)
            {
                _teleportRecoveryActive = true;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500, ct);
                        EnqueueWork(async () =>
                        {
                            try { await HandleTeleportAsync(ct); }
                            finally { _teleportRecoveryActive = false; }
                        });
                    }
                    catch (OperationCanceledException) { }
                }, ct);
            }
        });
    }

    // =====================================================================
    // LED helpers
    // =====================================================================

    private Task SubscribeLedAsync(string dataRefKey, EcamLed led)
    {
        return SubscribeEnqueuedAsync(GetDataRefPath(dataRefKey), (int v) =>
        {
            SetLedCached(led, v > 0 ? 1 : 0);
        });
    }

    private void SetLedCached(EcamLed led, int brightness)
    {
        brightness = Math.Clamp(brightness, 0, 255);
        _ledStateCache[led] = brightness;
        _hidDevice?.SetLed(led, brightness);
    }

    private void RestoreLedStates()
    {
        if (_hidDevice == null) return;
        foreach (var (led, brightness) in _ledStateCache)
            _hidDevice.SetLed(led, brightness);
    }

    // =====================================================================
    // Teleport recovery
    // =====================================================================

    private async Task HandleTeleportAsync(CancellationToken ct)
    {
        _logger.LogInformation("[ECAM] Teleport recovery — waiting for AirbusFBW alive");

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        IDisposable aliveSub = await _connector.SubscribeAsync(
            "xplanewebconnector/AirbusFBWalive",
            (int v) => { if (v == 1) tcs.TrySetResult(v); });

        try
        {
            await _connector.SetDataRefValueAsync("xplanewebconnector/AirbusFBWalive", 1);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            await tcs.Task.WaitAsync(cts.Token);
            _logger.LogInformation("[ECAM] AirbusFBW alive confirmed");
        }
        finally
        {
            aliveSub.Dispose();
        }

        RestoreLedStates();
        _logger.LogInformation("[ECAM] Teleport recovery complete");
    }
}
