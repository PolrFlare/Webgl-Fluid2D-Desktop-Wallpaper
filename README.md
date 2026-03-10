# WebGL Fluid 2D Desktop Wallpaper

Real-time **interactive fluid dynamics simulation** running directly on your Windows desktop as an animated wallpaper.

This project converts the browser-based **WebGL fluid simulation** created by **PavelDoGreat** into a **Windows desktop wallpaper application** using **C# WinForms + WebView2**.

The wallpaper renders the original WebGL simulation behind the desktop icons, allowing fluid interactions directly from your mouse movements.

---

## Preview

### Screenshot

![Wallpaper Screenshot](docs/screenshot-placeholder.png)

### Demo Video

![Demo Video](docs/demo-placeholder.gif)

*(Replace these placeholders with actual media once available.)*

---

## Original Project

This wallpaper is based on the WebGL simulation created by:

**PavelDoGreat – WebGL Fluid Simulation**

GitHub Repository:
https://github.com/PavelDoGreat/WebGL-Fluid-Simulation

All simulation rendering logic originates from that project.
This repository focuses on adapting the simulation into a **desktop wallpaper environment for Windows**.

---

## Features

* Real-time GPU fluid simulation
* Interactive mouse splashes
* Runs behind desktop icons
* System tray controls
* Optional pause when fullscreen applications are running
* Configurable simulation settings

---

## Requirements

* **Windows 10 / Windows 11**
* **.NET Framework / .NET compatible with WinForms**
* **Microsoft Edge WebView2 Runtime**

Download WebView2 Runtime if not already installed:
https://developer.microsoft.com/en-us/microsoft-edge/webview2/

---

## NuGet Dependencies

Before building the project, install these NuGet packages:

### WebView2

```
Microsoft.Web.WebView2
```

### JSON serialization

```
Newtonsoft.Json
```

You can install them via **Visual Studio NuGet Package Manager** or using the Package Manager Console:

```powershell
Install-Package Microsoft.Web.WebView2
Install-Package Newtonsoft.Json
```

---

## Building

1. Clone the repository

```
git clone https://github.com/YOUR_USERNAME/Webgl-Fluid2D-Desktop-Wallpaper.git
```

2. Open the solution in **Visual Studio**

3. Install the required **NuGet packages**

4. Build the project

```
Build → Build Solution
```

5. Run the application

The wallpaper will start and place the simulation behind the desktop icons.

---

## Controls

The application runs from the **system tray**.

Right-click the tray icon for options:

* Enable / Disable Wallpaper
* Start With Windows
* Pause When Fullscreen Application Is Running
* Exit

---

## Credits

Fluid Simulation:
**PavelDoGreat**
https://github.com/PavelDoGreat/WebGL-Fluid-Simulation

Desktop Wallpaper Implementation:
This repository

---

## License

Please refer to the original repository for licensing related to the WebGL simulation.
