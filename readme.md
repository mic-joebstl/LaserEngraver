# Laser Engraver
This project serves as an alternative to the software that comes with K3/K5 laser engravers.  

## Motivation
The original software only supports rasterized engraving, which limits its capabilities.  
This project aims to enhance the engraving process by analyzing the input image and determining the shortest path for the laser to follow, thereby potentially speeding up the process.  
However, due to the high latency of the engraver in acknowledging received commands and its quicker performance with single line engraving commands, this goal has not been fully realized yet.

## Installation
To install, either clone the repository or download the source code and compile the application using Visual Studio.  
Currently, only a Windows build profile is available as I don't use Linux with a GUI. Future releases will include prebuilt executables.
## How to use

### Engrave
Drag an image file into the window, click *Connect*, set your engraving preferences, and click *Start*.  

![demo](./res/demo.png)

The highlighted area represents what has already been engraved, while the dot indicates the current position of the laser.  

### Settings
- **Depth** determines how long the engraver stays at a position. The exact duration depends on the device and is specified here in percent.  
- **Variable burn intensity**: If enabled, burn intensity is determined by pixel brightness (including alpha channel).
- **Max. Power**: When **Variable burn intensity** is enabled, this option limits burn intensity to a maximum value.
- **Power**: Specifies fixed burn intensity for pixels to be engraved when **Variable burn intensity** is disabled.
- **Threshold**: Determines at what percent brightness a pixel is engraved when *Variable burn intensity* is disabled.
- **Rasterized**: Engraves pixels in rows in a grid pattern.
- **Raster optimized**: Begins engraving at any pixel and directly navigates to the next pixel to be engraved.
- **Show halo**: A visual setting that does not affect engraving.

### Keyboard Shortcuts
- `Ctrl` + `Left mouse button`: Moves the view (resets *Auto center view* setting).
- `Ctrl` + `Mouse wheel` and `Ctrl` + `+`/`-`: Adjusts zoom level.
- `Ctrl` + `0`: Resets zoom level.

## Configuration
All settings are stored in `appsettings.json`. If this file does not exist, it will be created at startup with default settings.  
While most settings can be configured via the UI, some (like custom themes and device settings) must be directly configured in the file.  
This section details settings that can only be set in `appsettings.json`.

### Example
```json
{
  "DeviceConfiguration": {
    "DPI": 510.54,
    "WidthDots": 1608,
    "HeightDots": 1608,
    "Type": 2
  },
  "UserConfiguration": {
    "Culture": "en",
    "ShowHelp": false,
    "AutoCenterView": true,
    "PreserveAspectRatio": true,
    "Unit": 1,
    "Theme": {
      "CanvasBackground": "#FFDDDDDD",
      "Foreground": "#FF111111",
      "SectionBackground": "#FFBBBBBB",
      "ErrorMessageForeground": "#FF750010",
      "BurnTargetBackground": "#FFFFFFFF",
      "BurnVisitedColor": "#FFA35000",
      "BurnStartColor": "#00000000",
      "BurnEndColor": "#FF000000"
    },
    "CustomTheme": null
  },
  "BurnConfiguration": {
    "Power": 255,
    "Duration": 255,
    "FixedIntensityThreshold": 127,
    "PlottingMode": 1,
    "IntensityMode": 1
  }
}
```

### DeviceConfiguration
This section should not be modified, as it is preconfigured for K3/K5 laser engravers.  
Other models are not supported as of now.  

- **DPI**: Dots per inch
- **WidthDots**/**HeightDots**: Width and height of the canvas
- **Type**: Set to 1 for demo-mode (default in debug-builds), which simulates a mockup device. Select 2 for USB devices (default in release-builds). Other devices are not supported.

### UserConfiguration
- **Culture**: The application is fully localized in both German (`de`) and English (`en`).
- **Theme**: This setting is overwritten when a theme (dark/light) is selected. If you wish to customize these settings, change `Theme` to `CustomTheme`. Otherwise, these settings will be overwritten.

### BurnConfiguration
This section specifies how the engraving should be performed.  
All of these settings can be configured using the UI.  

## Demo Mode
If you don't have a K3/K5 laser engraver or if you just want to try out this tool,  
set the `Type` setting in the `DeviceConfiguration` section in `appsettings.json` to 1.  
This will put the application in demo mode, simulating a mockup device.
