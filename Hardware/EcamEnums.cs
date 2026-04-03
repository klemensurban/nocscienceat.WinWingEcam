// Copyright (c) 2026 Klemens Urban <klemens.urban@outlook.com>
// SPDX-License-Identifier: GPL-3.0-only

namespace nocscienceat.WinWingEcam.Hardware;

[Flags]
public enum EcamDeviceMask
{
    None = 0,
    Ecam32 = 0x01,
}

public enum EcamLed
{
    PanelBacklight = 0,
    KeyBacklight = 1,
    EmerCanc = 3,
    Eng = 4,
    Bleed = 5,
    Press = 6,
    Elec = 7,
    Hyd = 8,
    Fuel = 9,
    Apu = 10,
    Cond = 11,
    Door = 12,
    Wheel = 13,
    Fctl = 14,
    ClrL = 15,
    Sts = 16,
    ClrR = 17,
    ToConfig = 20,
    Rcl = 21,
    All = 22,
}
