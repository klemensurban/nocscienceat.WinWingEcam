// Copyright (c) 2026 Klemens Urban <klemens.urban@outlook.com>
// SPDX-License-Identifier: GPL-3.0-only

using nocscienceat.WinWingEcam.Hardware;

namespace nocscienceat.WinWingEcam.Panels;

public partial class EcamPanelHandler
{
    private Dictionary<int, (string Label, string CommandKey)> _buttonMap = [];

    protected override void RegisterDataRefsAndCommands()
    {
        RegisterCommands(
        [
            ("TOConfigPress", "AirbusFBW/TOConfigPress"),
            ("EmerCancel", "AirbusFBW/EmerCancel"),
            ("SelectEnginePage", "AirbusFBW/ECP/SelectEnginePage"),
            ("SelectBleedPage", "AirbusFBW/ECP/SelectBleedPage"),
            ("SelectPressPage", "AirbusFBW/ECP/SelectPressPage"),
            ("SelectElecACPage", "AirbusFBW/ECP/SelectElecACPage"),
            ("SelectHydraulicPage", "AirbusFBW/ECP/SelectHydraulicPage"),
            ("SelectFuelPage", "AirbusFBW/ECP/SelectFuelPage"),
            ("SelectAPUPage", "AirbusFBW/ECP/SelectAPUPage"),
            ("SelectConditioningPage", "AirbusFBW/ECP/SelectConditioningPage"),
            ("SelectDoorOxyPage", "AirbusFBW/ECP/SelectDoorOxyPage"),
            ("SelectWheelPage", "AirbusFBW/ECP/SelectWheelPage"),
            ("SelectFlightControlPage", "AirbusFBW/ECP/SelectFlightControlPage"),
            ("ECAMAll", "AirbusFBW/ECAMAll"),
            ("CaptainClear", "AirbusFBW/ECP/CaptainClear"),
            ("SelectStatusPage", "AirbusFBW/ECP/SelectStatusPage"),
            ("ECAMRecall", "AirbusFBW/ECAMRecall"),
            ("CopilotClear", "AirbusFBW/ECP/CopilotClear"),
        ]);

        RegisterDataRefs(
        [
            ("PanelBrightness", "AirbusFBW/PanelBrightnessLevel"),
            ("CLRillum", "AirbusFBW/CLRillum"),
            ("SDENG", "AirbusFBW/SDENG"),
            ("SDBLEED", "AirbusFBW/SDBLEED"),
            ("SDPRESS", "AirbusFBW/SDPRESS"),
            ("SDELEC", "AirbusFBW/SDELEC"),
            ("SDHYD", "AirbusFBW/SDHYD"),
            ("SDFUEL", "AirbusFBW/SDFUEL"),
            ("SDAPU", "AirbusFBW/SDAPU"),
            ("SDCOND", "AirbusFBW/SDCOND"),
            ("SDDOOR", "AirbusFBW/SDDOOR"),
            ("SDWHEEL", "AirbusFBW/SDWHEEL"),
            ("SDFCTL", "AirbusFBW/SDFCTL"),
            ("SDSTATUS", "AirbusFBW/SDSTATUS"),
        ]);
    }

    private void CreateButtonDispatchTable()
    {
        _buttonMap = new Dictionary<int, (string Label, string CommandKey)>
        {
            [1] = ("TO_CONFIG", "TOConfigPress"),
            [3] = ("EMER_CANC", "EmerCancel"),
            [4] = ("ENG", "SelectEnginePage"),
            [5] = ("BLEED", "SelectBleedPage"),
            [6] = ("PRESS", "SelectPressPage"),
            [7] = ("ELEC", "SelectElecACPage"),
            [8] = ("HYD", "SelectHydraulicPage"),
            [9] = ("FUEL", "SelectFuelPage"),
            [10] = ("APU", "SelectAPUPage"),
            [11] = ("COND", "SelectConditioningPage"),
            [12] = ("DOOR", "SelectDoorOxyPage"),
            [13] = ("WHEEL", "SelectWheelPage"),
            [14] = ("FCTL", "SelectFlightControlPage"),
            [15] = ("ALL", "ECAMAll"),
            [16] = ("CLR_L", "CaptainClear"),
            [18] = ("STS", "SelectStatusPage"),
            [19] = ("RCL", "ECAMRecall"),
            [21] = ("CLR_R", "CopilotClear"),
        };
    }
}
