# nocscienceat.WinWingEcam

WinWing ECAM (Engine/Warning Display Control Panel) driver for X-Plane, built on the `nocscienceat.XPlanePanel` / `nocscienceat.XPlaneWebConnector` framework.

Drives the WinWing ECAM hardware via USB HID — reads 18 ECAM page selector buttons and drives 17 annunciator LEDs. All button presses bridge to X-Plane commands via the ToLiss AirbusFBW plugin.

## Installation

```bash
dotnet add package nocscienceat.WinWingEcam
```

## Features

- **ECAM page selectors** — ENG, BLEED, PRESS, ELEC, HYD, FUEL, APU, COND, DOOR, WHEEL, FCTL with active-page LEDs
- **Status/recall** — STS, RCL, ALL buttons
- **Clear** — CLR L (captain) and CLR R (copilot) with illumination LEDs
- **TO CONFIG / EMER CANC** — annunciator buttons
- **Panel brightness** — driven by `AirbusFBW/PanelBrightnessLevel`
- **USB disconnect/reconnect** — automatic recovery with LED state restore
- **Teleport detection** — re-syncs after aircraft reload

## Usage

This library provides a panel handler for WinWing ECAM hardware,
integrating with X-Plane through the nocscienceat.XPlanePanel framework.

### Basic Setup

Create a hosted service application with the following `Program.cs`:

```csharp
using nocscienceat.WinWingEcam.Panels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nocscienceat.XPlanePanel;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("datarefs.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("XP_");

// Core services
builder.Services.AddXPlaneWebConnector(builder.Configuration);
builder.Services.AddXPlanePanel();

// Panel handlers (register all, filter by config at runtime)
builder.Services.AddSingleton<IPanelHandler, EcamPanelHandler>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var host = builder.Build();
await host.RunAsync();
```

### Configuration

Create an `appsettings.json` file with your X-Plane Web Connector settings
and panel configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "System.Net.Http.HttpClient": "Warning"
    }
  },
  "XPlane": {
    "IpAddress": "127.0.0.1",
    "WebPort": 8086,
    "ReadinessProbeDataRef": "AirbusFBW/BatVolts",
    "ReadinessProbeMaxRetries": 0,
    "Transport": "Http",
    "FireForgetOnHttpTransport": true,
    "ApiVersion": "v2"
  },
  "Panels": {
    "ECAM": {
      "Enabled": true
    }
  }
}
```

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Enabled` | bool | false | Enable/disable the ECAM panel |
| `VendorId` | int | 0x4098 | USB Vendor ID |
| `ProductId` | int | 0 | USB Product ID. `0` = auto-detect, `48000` (0xBB70) = force |

### DataRefs Configuration (Optional)

Optionally create a `datarefs.json` file to override the default
ToLiSS Airbus dataref and command mappings (the panel includes all
required definitions):

```json
{
  "XplaneDataRefsCommands": {
    "ECAM": {
      "DataRefs": {
      },
      "Commands": {
      }
    }
  }
}
```

## Protocol Documentation

See [docs/ECAM-Design-Document.md](docs/ECAM-Design-Document.md) for the full protocol specification.

## License

This project is licensed under the **GNU General Public License v3.0**
(GPL-3.0). See the [LICENSE](LICENSE) file for details.

### Attribution

This project is a C# port derived from
[XSchenFly](https://github.com/schenlap/XSchenFly) by **memo5@gmx.at**,
originally released under the GPL-3.0 license. The HID protocol
implementation and X-Plane dataref mappings are based on that work.

C# port and modifications by **Klemens Urban**
(<klemens.urban@outlook.com>, https://github.com/klemensurban).

### Dependencies

This project depends on the
[nocscienceat.XPlanePanel](https://github.com/klemensurban/nocscienceat.XPlanePanel)
framework and the
[nocscienceat.XPlaneWebConnector](https://github.com/klemensurban/nocscienceat.XPlaneWebConnector),
which are licensed separately under the **MIT License** by Klemens Urban.

## Repository

https://github.com/klemensurban/nocscienceat.WinWingEcam
