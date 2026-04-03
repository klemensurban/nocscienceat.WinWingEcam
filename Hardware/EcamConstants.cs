// Copyright (c) 2026 Klemens Urban <klemens.urban@outlook.com>
// SPDX-License-Identifier: GPL-3.0-only

namespace nocscienceat.WinWingEcam.Hardware;

public static class EcamConstants
{
    public const int ButtonCount = 22;
    public const byte IdentifierByte = 0x70;
    public const byte ReportId = 0x01;
    public const int ReportButtonOffset = 1;
    public const int ReportButtonBytes = 4;

    /// <summary>Valid input report lengths accepted from the device.</summary>
    //public static readonly HashSet<int> ValidReportLengths = [12, 14, 33, 64];
    public static readonly HashSet<int> ValidReportLengths = [64];

    public static readonly (int Pid, string Name, EcamDeviceMask Mask)[] KnownDevices =
    [
        (0xBB70, "WINWING ECAM", EcamDeviceMask.Ecam32),
    ];
}
