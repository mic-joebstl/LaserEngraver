# Laser Engraver
Intended as an alternative to the software that is shipped with K3/K5 laser engravers.  

## Why
The original software is limited in its capabilities as it supports rasterized engraving only.  
I intend to speed up the engraving process with this tool by analyzing the provided image and finding the shortest path for the laser to follow.  
So far I have not quite achieved this goal yet, as the engraver has a high latency for acknowleging received commands and performs single line engraving commands more quickly.  

## Installation
Clone the repo or download the source and compile the application using Visual Studio.  
As of now, there is only a build profile for Windows, as I don't use Linux with a GUI.  
In the future there will be releases with prebuilt executables.  

## How to use

### Engrave
To do this, drag an image file into the window, press *Connect*, set your engraving preferences and *Start*.  

![demo](./res/demo.png)

The highlighted area shows what has been engraved already,  
and the dot shows the current position of the laser.  

### Settings
*Depth* sets the duration for which the engraver stays at a position.  
Exact duration depends on the device, so it is specified here in percent.  

- *Variable burn intensity*: If active, the burn intensity is determined by the brightness of the pixel (including alpha channel).
- *Max. Power* limits the burn intensity to a maximum value when *Variable burn intensity* is active.
- *Power* specifies the fixed burn intensity of the pixels to be engraved if *Variable burn intensity* is disabled.
- *Threshold* is the value in percent at which brightness a pixel is engraved if *Variable burn intensity* is disabled.
- *Rasterized* engraves the pixels in rows in a grid pattern.
- *Raster optimized* starts engraving at any pixel and navigates directly to the next pixel to be engraved from there.
- *Show halo* is a purely visual setting and has no effect on engraving.

### Keyboard shortcuts
- `Ctrl` + `Left mouse button` moves the view. The *Auto center view* setting is reset.
- `Ctrl` + `Mouse wheel` and `Ctrl` + `+`/`-` sets the zoom level.
- `Ctrl` + `0` resets the zoom level.

## Configuration
All settings are stored in `appsettings.json`.  
If the file doesn't exist, it's created at startup with default settings.  
Most settings can be configured using the UI, but some (like custom themes and device settings) must be configured in the file directly.  
In this section I will only go into detail for settings that can only be set in `appsettings.json`.  

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

- DPI: dots per inch
- WidthDots/HeightDots: width and height of the canvas
- Type: When set to 1 (default in debug-builds), the application is in demo-mode (i.e. a mockup device is simulated). Select 2 (default in release-builds) for USB devices. Other devices are not supported yet.

### UserConfiguration
- Culture: The application is fully localized in german `de` and english `en`.  
- Theme: This setting is overwritten when selecting a theme (dark/light). If you wish to customize these settings, change `Theme` to `CustomTheme`, otherwise these settings will be overwritten.  

### BurnConfiguration
Specifies how the engraving should be performed.  
All of these settings can be configured using the UI.  

## Demo
If you don't have a K3/K5 laser engraver or just want to try this tool out,  
set the `Type` setting in the `DeviceConfiguration` section in `appsettings.json` to 1.  
This will put the application in demo-mode, i.e. simulate a mockup-device.  