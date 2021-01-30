# KSPBugReport

### Bug reporting plugin for KSP players

### Features :
Add options to the KSP debug window (ALT+F12) to :
- Create a zipped bug report on the desktop, containing :
  - The `KSP.log` file
  - The `modulemanager.configcache` file, or if not found a `Configs.txt` file generated from the live game database
  - A `*.sfs` savegame generated from the currently loaded game (if any)
  - The `Logs\Kopernicus` body configs dumps (if present)
- Take screenshots and add them to the last created report
- Upload the zipped report to a file sharing service :
  - Primary : https://oshi.at (90 days retention, unlimited downloads)
  - Secondary : https://0x0.st (30 days minimum retention, max 1 year depending on file size, unlimited downloads)
  - Tertiary : https://file.io (14 days retention, file deleted after the first download)
- Shortcuts to copy the dowload link to clipboard and to open the report folder in the system file explorer
  
Compatible with KSP 1.8+

![screenshot](https://github.com/HarmonyKSP/KSPBugReport/raw/master/Screenshot.png)

### License
MIT

### Notes
Zip compression is done through the `System.IO.Compression` mono library as provided in the Unity 2019.2.2f1 editor download in `Editor\Data\MonoBleedingEdge\lib\mono\4.5`
