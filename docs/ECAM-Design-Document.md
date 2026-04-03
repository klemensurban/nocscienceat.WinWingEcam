# WinWing ECAM Panel — Design Document

## 1. Overview

The **WinWing ECAM** (Engine/Warning Display Control Panel) is a USB HID device providing:
- **18 buttons** — ECAM page selectors (ENG, BLEED, PRESS, ELEC, HYD, FUEL, APU, COND, DOOR, WHEEL, FCTL), status/recall (STS, RCL, ALL), clear (CLR L, CLR R), TO CONFIG, and EMER CANC
- **15 annunciator LEDs** — one per page selector button plus CLR L/R, STS, TO CONFIG, RCL, ALL, EMER CANC
- **2 brightness LEDs** — panel backlight and key backlight

No LCD display. All buttons fire short commands on press only. All LEDs are driven by direct dataref subscriptions.

### Source reference

Protocol and button/LED mappings from [ECAM_py.txt](../../ECAM_py.txt), based on WINCTRL conventions.

---

## 2. Device Detection

| Device | VID | PID | Identifier byte | Name |
|--------|-----|-----|-----------------|------|
| WINWING ECAM | 0x4098 | 0xBB70 | 0x70 | ECAM32 |

Single device variant. No Captain/FO distinction.

---

## 3. HID Input — Button Reports

- **Report ID:** 0x01 (byte 0)
- **Valid report length:** 12 bytes
- **Button bitmask:** bytes 1-4 form a single 32-bit word (little-endian)
- **Total buttons:** 22 (18 mapped, 4 hidden/unused)

### Button ID to action mapping

| ID | Label | Command |
|----|-------|---------|
| 1 | TO_CONFIG | `AirbusFBW/TOConfigPress` |
| 3 | EMER_CANC | `AirbusFBW/EmerCancel` |
| 4 | ENG | `AirbusFBW/ECP/SelectEnginePage` |
| 5 | BLEED | `AirbusFBW/ECP/SelectBleedPage` |
| 6 | PRESS | `AirbusFBW/ECP/SelectPressPage` |
| 7 | ELEC | `AirbusFBW/ECP/SelectElecACPage` |
| 8 | HYD | `AirbusFBW/ECP/SelectHydraulicPage` |
| 9 | FUEL | `AirbusFBW/ECP/SelectFuelPage` |
| 10 | APU | `AirbusFBW/ECP/SelectAPUPage` |
| 11 | COND | `AirbusFBW/ECP/SelectConditioningPage` |
| 12 | DOOR | `AirbusFBW/ECP/SelectDoorOxyPage` |
| 13 | WHEEL | `AirbusFBW/ECP/SelectWheelPage` |
| 14 | FCTL | `AirbusFBW/ECP/SelectFlightControlPage` |
| 15 | ALL | `AirbusFBW/ECAMAll` |
| 16 | CLR_L | `AirbusFBW/ECP/CaptainClear` |
| 18 | STS | `AirbusFBW/ECP/SelectStatusPage` |
| 19 | RCL | `AirbusFBW/ECAMRecall` |
| 21 | CLR_R | `AirbusFBW/ECP/CopilotClear` |

IDs 0, 2, 17, 20 are hidden/unused positions.

All buttons: `SendCommandAsync(path, 0.15f)` on press only. No release actions.

---

## 4. HID Output — LED Control

### Packet format (14 bytes)

```
[0x02, 0x70, 0xBB, 0x00, 0x00, 0x03, 0x49, <led_id>, <brightness>, 0, 0, 0, 0, 0]
```

Identifier byte `0x70`.

### LED IDs

| ID | Name | Range | Description |
|----|------|-------|-------------|
| 0 | PANEL_BACKLIGHT | 0-255 | Panel backlight |
| 1 | KEY_BACKLIGHT | 0-255 | Key backlight |
| 3 | EMER_CANC | 0/1 | Emergency cancel annunciator |
| 4 | ENG | 0/1 | Engine page active |
| 5 | BLEED | 0/1 | Bleed page active |
| 6 | PRESS | 0/1 | Press page active |
| 7 | ELEC | 0/1 | Elec page active |
| 8 | HYD | 0/1 | Hydraulic page active |
| 9 | FUEL | 0/1 | Fuel page active |
| 10 | APU | 0/1 | APU page active |
| 11 | COND | 0/1 | Conditioning page active |
| 12 | DOOR | 0/1 | Door page active |
| 13 | WHEEL | 0/1 | Wheel page active |
| 14 | FCTL | 0/1 | Flight control page active |
| 15 | CLR_L | 0/1 | Captain clear active |
| 16 | STS | 0/1 | Status page active |
| 17 | CLR_R | 0/1 | Copilot clear active |
| 20 | TO_CONFIG | 0/1 | TO Config annunciator |
| 21 | RCL | 0/1 | Recall annunciator |
| 22 | ALL | 0/1 | All pages annunciator |

### LED source datarefs

| LED(s) | Dataref | Logic |
|--------|---------|-------|
| PANEL_BACKLIGHT, KEY_BACKLIGHT | `AirbusFBW/PanelBrightnessLevel` | `int(float * 255)` |
| CLR_L, CLR_R | `AirbusFBW/CLRillum` | Direct (same value drives both) |
| ENG | `AirbusFBW/SDENG` | Direct (0/1) |
| BLEED | `AirbusFBW/SDBLEED` | Direct |
| PRESS | `AirbusFBW/SDPRESS` | Direct |
| ELEC | `AirbusFBW/SDELEC` | Direct |
| HYD | `AirbusFBW/SDHYD` | Direct |
| FUEL | `AirbusFBW/SDFUEL` | Direct |
| APU | `AirbusFBW/SDAPU` | Direct |
| COND | `AirbusFBW/SDCOND` | Direct |
| DOOR | `AirbusFBW/SDDOOR` | Direct |
| WHEEL | `AirbusFBW/SDWHEEL` | Direct |
| FCTL | `AirbusFBW/SDFCTL` | Direct |
| STS | `AirbusFBW/SDSTATUS` | Direct |

TO_CONFIG, EMER_CANC, RCL, ALL LEDs: no confirmed datarefs in the Python source (commented out as placeholders).

---

## 5. Dataref Subscriptions

Total: **~14 subscriptions** — the lightest panel in the solution.

| Dataref | Type | Used for |
|---------|------|----------|
| `AirbusFBW/PanelBrightnessLevel` | float | Backlight (both panel + key) |
| `AirbusFBW/CLRillum` | float | CLR L + CLR R LEDs |
| `AirbusFBW/SDENG` | float | ENG LED |
| `AirbusFBW/SDBLEED` | float | BLEED LED |
| `AirbusFBW/SDPRESS` | float | PRESS LED |
| `AirbusFBW/SDELEC` | float | ELEC LED |
| `AirbusFBW/SDHYD` | float | HYD LED |
| `AirbusFBW/SDFUEL` | float | FUEL LED |
| `AirbusFBW/SDAPU` | float | APU LED |
| `AirbusFBW/SDCOND` | float | COND LED |
| `AirbusFBW/SDDOOR` | float | DOOR LED |
| `AirbusFBW/SDWHEEL` | float | WHEEL LED |
| `AirbusFBW/SDFCTL` | float | FCTL LED |
| `AirbusFBW/SDSTATUS` | float | STS LED |

---

## 6. Architecture

### Project structure

```
nocscienceat.WinWingEcam/
  Configuration/
    WinWingEcamConfig.cs
  Hardware/
    EcamConstants.cs
    EcamEnums.cs
    WinWingEcamHidDevice.cs
  Panels/
    EcamPanelHandler.cs
    EcamPanelHandlerRegDataRefCommand.cs
    EcamPanelHandlerWireDataRefCommand.cs
  docs/
    ECAM-Design-Document.md
  LICENSE
  README.md
  nocscienceat.WinWingEcam.csproj
```

### Key design decisions

1. **Simplest panel** — no LCD, no special button logic, no annunciator test. All buttons fire short commands, all LEDs map 1:1 to datarefs.

2. **Same USB patterns** — disconnect/reconnect with fresh device, LED state cache for restore, teleport recovery.

3. **32-bit bitmask** — single `uint` for buttons (unlike AGP's 64+32 split).

4. **CLRillum drives two LEDs** — single dataref subscription updates both CLR_L and CLR_R.

---

## 7. Configuration

```json
{
  "Panels": {
    "ECAM": {
      "Enabled": true
    }
  }
}
```
