// Copyright (c) 2026 Klemens Urban <klemens.urban@outlook.com>
// SPDX-License-Identifier: GPL-3.0-only

using System.Buffers;
using System.ComponentModel;
using System.Threading.Channels;
using HidSharp;
using Microsoft.Extensions.Logging;

namespace nocscienceat.WinWingEcam.Hardware;

/// <summary>
/// Manages the USB HID connection to a WinWing ECAM device.
/// </summary>
public sealed class WinWingEcamHidDevice : IDisposable
{
    private readonly ILogger _logger;
    private HidStream? _stream;
    private Task? _writeTask;
    private Task? _readTask;

    private readonly Channel<byte[]> _hidWriteChannel =
        Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });

    public event Action<byte[], int>? ReportReceived;
    public event Action? Disconnected;

    public EcamDeviceMask DeviceMask { get; private set; }
    public bool IsOpen => _stream != null;

    public WinWingEcamHidDevice(ILogger logger)
    {
        _logger = logger;
    }

    public bool FindAndOpen(int vid, int forcePid)
    {
        if (forcePid != 0)
        {
            var device = DeviceList.Local.GetHidDevices(vid, forcePid).FirstOrDefault();
            if (device != null)
            {
                var known = EcamConstants.KnownDevices.FirstOrDefault(d => d.Pid == forcePid);
                DeviceMask = known.Mask != 0 ? known.Mask : EcamDeviceMask.Ecam32;
                _stream = device.Open();
                _logger.LogInformation("[ECAM] Opened forced device PID 0x{Pid:X4}", forcePid);
                return true;
            }
            _logger.LogWarning("[ECAM] Forced PID 0x{Pid:X4} not found", forcePid);
            return false;
        }

        foreach (var (pid, name, mask) in EcamConstants.KnownDevices)
        {
            _logger.LogDebug("[ECAM] Searching for WinWing {Name} ...", name);
            var device = DeviceList.Local.GetHidDevices(vid, pid).FirstOrDefault();
            if (device != null)
            {
                DeviceMask = mask;
                _stream = device.Open();
                _logger.LogInformation("[ECAM] Found WinWing {Name}", name);
                return true;
            }
        }

        _logger.LogWarning("[ECAM] No compatible WinWing ECAM device found");
        return false;
    }

    public void StartIo(CancellationToken ct)
    {
        _writeTask = Task.Run(() => DrainWriteChannelAsync(ct), ct);
        _readTask = Task.Run(() => ReadLoopAsync(ct), ct);
    }

    public void SetLed(EcamLed led, int brightness)
    {
        brightness = Math.Clamp(brightness, 0, 255);
        byte[] data = [0x02, EcamConstants.IdentifierByte, 0xBB, 0, 0, 3, 0x49, (byte)led, (byte)brightness, 0, 0, 0, 0, 0];
        _hidWriteChannel.Writer.TryWrite(data);
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_stream == null) break;
            try
            {
                byte[] report = ArrayPool<byte>.Shared.Rent(64);
                try
                {
                    int totalBytes = await _stream.ReadAsync(report.AsMemory(0, 64), ct);
                    if (totalBytes > 0)
                        ReportReceived?.Invoke(report, totalBytes);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(report);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug("[ECAM] HID read stopped (stream closed)");
                break;
            }
            catch (IOException ex) when (ex.InnerException is Win32Exception { NativeErrorCode: 1167 })
            {
                _logger.LogWarning("[ECAM] HID device disconnected: {Message}", ex.Message);
                CloseStream();
                Disconnected?.Invoke();
                break;
            }
            catch (IOException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ECAM] HID read error");
                await Task.Delay(50, ct);
            }
        }
    }

    private async Task DrainWriteChannelAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var packet in _hidWriteChannel.Reader.ReadAllAsync(ct))
            {
                if (_stream == null) continue;
                try
                {
                    _stream.Write(packet);
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogDebug("[ECAM] HID write stopped (stream closed)");
                    break;
                }
                catch (IOException ex) when (ex.InnerException is Win32Exception { NativeErrorCode: 1167 })
                {
                    _logger.LogWarning("[ECAM] HID write: device disconnected");
                    CloseStream();
                    Disconnected?.Invoke();
                    break;
                }
                catch (IOException) { }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[ECAM] HID write error");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    public void Dispose()
    {
        _hidWriteChannel.Writer.TryComplete();
        try { _writeTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
        try { _readTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
        CloseStream();
    }

    private void CloseStream()
    {
        var s = Interlocked.Exchange(ref _stream, null);
        try { s?.Close(); } catch { }
        try { s?.Dispose(); } catch { }
    }
}
