# SVS-PelvisFin

plugin to modify miscellaneous character status in character creation and h scene for SamabakeScramble.

## Prerequisites (Aicomi)

- [AC-HF_Patch](https://github.com/ManlyMarco/AC-HF_Patch)
  - Message Center
  - BepInEx.ConfigurationManager
  - SVS_BepisPlugins
- [CoastalSmell](https://github.com/MaybeSamigroup/SVS-Fishbone)
  - 1.1.9 or later

Confirmed working under Aicomi 1.0.1

## Prerequisites (SamabakeScramble)

- [SVS-HF_Patch](https://github.com/ManlyMarco/SVS-HF_Patch)
  - Message Center
  - BepInEx.ConfigurationManager
  - SVS_BepisPlugins
- [CoastalSmell](https://github.com/MaybeSamigroup/SVS-Fishbone)
  - 1.1.9 or later

Confirmed working under SamabakeScramble 1.1.6

## Installation

Extract the [latest release](https://github.com/MaybeSamigroup/SVS-PelvicFin/releases/latest) to your game install directory.

## Migration from older release

Remove `PelvicFin.dll` from BepinEx/plugins.

Plugin assembly names are now `SVS_PelvicFin.dll` and `AC_PelvicFin.dll`.

## How to use

Start character creation or h scene in game; you'll see additional UI.

To show / hide the ui, you can use keyboard shortcut.

Ctrl + p is mapped as default and can be configured through plugin setting.

## Tips

Base color setting for simple figure is only applied to player character.

So, you should modify n_body_silhouette/n_tang_silhouette color properties of others via [SardineHead](https://github.com/MaybeSamigroup/SVS-SardineHead).
