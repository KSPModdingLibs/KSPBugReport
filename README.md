# KSPBugReport

### Bug reporting plugin for KSP players

This small plugin adds a "bug report" menu to the KSP debug menu (opened with ALT+F12). That menu provide a way for players to create a zipped bug report cointaining the KSP logs and game database dumps usually needed by mod authors when they need to troubleshot bugs and issues. It also include an option to automatically upload the bug report to an online file sharing service.

![screenshot](https://github.com/HarmonyKSP/KSPBugReport/raw/master/Screenshot.png)

### Dowload and installation

Compatible with KSP 1.8+

KSPBugReport is available on [CKAN](https://github.com/KSP-CKAN/CKAN) (recommended)

For manual installation :

- Go to the **[GitHub release page](https://github.com/KSPModdingLibs/KSPBugReport/releases)** and download the file named `KSPBugReport_x.x.x_KSP1.8+.zip`
- Open the downloaded *.zip archive
- Copy the `KSPBugReport` folder found in the archive into the `GameData` folder of your KSP installation.

### How to use

- Try to reproduce your issue or bug, in order for KSP to log information about what happened.
- Press ALT+F12
- In the `Bug Report` section, click on `Create report`. This will create a zip archive on your desktop. You can open the location of that zip file in your file explorer by clicking on `Open report folder`.
- If you want to add screenshots to your report, open/position what you want to show and click on `Add screenshot to last report`. The debug menu window won't appear on the screenshot. You can add multiple screenshots.
- To upload the report to a file sharing service, click on `Upload last report`. Once uploaded, the download link to your report will be copied to the clipboard and you can paste it elsewhere (forum post, github, discord...). In case you loose the copied link, you can copy it to the clipboard again by clicking on `Copy last upload link to clipboard`.

### Disclaimer

This plugin **will upload information about your computer specs, your KSP install and your current game on the internet**, on public file sharing services. 

The KSP logs usually don't contain any sensitive information, however, be aware that :
- The KSP.log file contain the path to your KSP install, which **can contain the user name for your computer** (for example `C:\Users\MyUserName\...\KSP\`). This is usually not a security issue, but if you want to avoid that, move your KSP folder to another path (for example `C:\Games\KSP\`).
- The KSP.log file contain everything that is logged by your mods/plugins. An ill-intentioned mod author can potentially access any file on your computer and dump sensitive information in the KSP logs.
- Your savegame is included in the report, with everything that you can write and save in KSP (vessel names and descriptions for example)

### Notes :

- Bug reports will contain :
  - The `KSP.log` file
  - The `modulemanager.configcache` file, or if not found a `Configs.txt` file generated from the live game database
  - A `*.sfs` savegame generated from the currently loaded game (if any)
  - The `Logs\Kopernicus` body configs dumps (if present)
- Available upload services :
  - Primary : https://oshi.at (90 days retention, unlimited downloads)
  - Secondary : https://0x0.st (30 days minimum retention, max 1 year depending on file size, unlimited downloads)
  - Tertiary : https://file.io (14 days retention, file deleted after the first download)
- Zip compression is done through the `System.IO.Compression` mono library as provided in the Unity 2019.2.2f1 editor download in `Editor\Data\MonoBleedingEdge\lib\mono\4.5`

### License
MIT
