// Copyright (c) 2026 Klemens Urban <klemens.urban@outlook.com>
// SPDX-License-Identifier: GPL-3.0-only

using nocscienceat.XPlanePanel.Configuration;

namespace nocscienceat.WinWingEcam.Configuration;

public class WinWingEcamConfig : PanelConfig
{
    /// <summary>USB Vendor ID for WinWing devices. Default 0x4098.</summary>
    public int VendorId { get; set; } = 0x4098;

    /// <summary>USB Product ID. 0 = auto-detect from known device list.</summary>
    public int ProductId { get; set; } = 0;
}
