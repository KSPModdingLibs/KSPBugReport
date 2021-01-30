using System;
using System.Collections;
using System.IO;
using System.Reflection;
using KSP.UI;
using KSP.UI.Screens.DebugToolbar;
using UnityEngine;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace KSPBugReport
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class KSPBugReport : MonoBehaviour
    {
        private static List<UITreeView.Item> debugScreenOptions;

        // Adding the ALT+F12 menu options trough DebugScreen.AddContentItem() will add them to beginning of the list unless the 
        // DebugScreen instance has already been fully initialized once, which happens the first time it is opened.
        // So we delay the options creation until we detect that the other options are already created.

        private void Start()
        {
            DontDestroyOnLoad(this);

            try
            {
                debugScreenOptions = (List<UITreeView.Item>)typeof(DebugScreen).GetField("treeItems", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                if (debugScreenOptions == null)
                {
                    throw new Exception("Error creating bug report options : DebugScreen options list not found");
                }
            }
            catch (Exception e)
            {
                Destroy(this);
                Lib.Log(e.ToString(), Lib.LogLevel.Error);
            }
        }

        private void Update()
        {
            if (debugScreenOptions.Count > 0)
            {
                UITreeView.Item parent = DebugScreen.AddContentItem(null, "bugreport", "Bug report", null, null);
                DebugScreen.AddContentItem(parent, "createreport", "Create report", null, CreateReportFromDebugScreen);
                DebugScreen.AddContentItem(parent, "addscreenshot", "Add screenshot to last report", null, AddScreenshotToLastReport);
                DebugScreen.AddContentItem(parent, "uploadreport", "Upload last report", null, UploadLastReport);
                DebugScreen.AddContentItem(parent, "copylinktoclipoard", "Copy last upload link to clipboard", null, CopyLastLinkToClipboard);
                DebugScreen.AddContentItem(parent, "openreport", "Open last report", null, OpenReportFromDebugScreen);
                DebugScreen.AddContentItem(parent, "openfolder", "Open report folder", null, OpenFolderFromDebugScreen);

                Destroy(this);
            }
        }

        #region DEBUG MENU EXTRA OPTIONS

        private static string lastReportPath = string.Empty;
        private static string lastReportFolderPath = string.Empty;
        public static string lastReportUploadURI = string.Empty;

        private static void CreateReportFromDebugScreen()
        {
            lastReportUploadURI = string.Empty;

            if (CreateReport(out string zipFileName, out lastReportFolderPath, out lastReportPath))
            {
                ScreenMessages.PostScreenMessage($"Bug report :\n<color=#FF8000>{zipFileName}</color>\ncreated in folder :\n<color=#FF8000>{lastReportFolderPath}</color>", 10f, ScreenMessageStyle.UPPER_CENTER, true);
            }
            else
            {
                ScreenMessages.PostScreenMessage($"Error while creating bug report", 3f, ScreenMessageStyle.UPPER_CENTER, Color.red);
            }
        }

        private static void OpenReportFromDebugScreen()
        {
            if (!string.IsNullOrEmpty(lastReportPath) && File.Exists(lastReportPath))
            {
                System.Diagnostics.Process.Start(lastReportPath);
            }
            else
            {
                ScreenMessages.PostScreenMessage($"No bug report to open, please create one first.", 3f, ScreenMessageStyle.UPPER_CENTER, Color.red);
            }
        }

        private static void OpenFolderFromDebugScreen()
        {
            if (!string.IsNullOrEmpty(lastReportFolderPath) && Directory.Exists(lastReportFolderPath))
            {
                System.Diagnostics.Process.Start(lastReportFolderPath);
            }
            else
            {
                ScreenMessages.PostScreenMessage($"No bug report to open, please create one first.", 3f, ScreenMessageStyle.UPPER_CENTER, Color.red);
            }
        }

        private static void UploadLastReport()
        {
            if (!string.IsNullOrEmpty(lastReportPath) && File.Exists(lastReportPath))
            {
                HighLogic.fetch.StartCoroutine(Upload());
            }
            else
            {
                ScreenMessages.PostScreenMessage($"No bug report to upload, please create one first.", 3f, ScreenMessageStyle.UPPER_CENTER, Color.red);
            }
        }

        private static IEnumerator Upload()
        {
            byte[] fileData = File.ReadAllBytes(lastReportPath);
            string fileName = Path.GetFileName(lastReportPath);

            UploadService primaryService = new UploadServiceOshi(fileData, fileName);
            yield return HighLogic.fetch.StartCoroutine(primaryService.Send());

            if (primaryService.Success)
            {
                yield break;
            }

            UploadService secondaryService = new UploadService0x0(fileData, fileName);
            yield return HighLogic.fetch.StartCoroutine(secondaryService.Send());

            if (secondaryService.Success)
            {
                yield break;
            }

            UploadService tertiaryService = new UploadServiceFileio(fileData, fileName);
            yield return HighLogic.fetch.StartCoroutine(tertiaryService.Send());
        }

        public static void CopyLastLinkToClipboard()
        {
            if (!string.IsNullOrEmpty(lastReportUploadURI))
            {
                GUIUtility.systemCopyBuffer = lastReportUploadURI;
                ScreenMessages.PostScreenMessage($"Link <color=#FF8000>{lastReportUploadURI}</color> copied to clipboard", 15f, ScreenMessageStyle.UPPER_CENTER, true);
                Lib.Log($"Download link '{lastReportUploadURI}' for {Path.GetFileName(lastReportPath)} copied to clipboard");
            }
            else
            {
                ScreenMessages.PostScreenMessage($"No bug report uploaded, please upload one first.", 3f, ScreenMessageStyle.UPPER_CENTER, Color.red);
            }
        }

        private static void AddScreenshotToLastReport()
        {
            if (!string.IsNullOrEmpty(lastReportPath) && File.Exists(lastReportPath))
            {
                HighLogic.fetch.StartCoroutine(TakeScreenshot());
            }
            else
            {
                ScreenMessages.PostScreenMessage($"No bug report to add a screenshot to, please create one first.", 3f, ScreenMessageStyle.UPPER_CENTER, Color.red);
            }
        }

        #endregion

        #region BUG REPORT CREATION

        private static bool CreateReport(out string zipFileName, out string zipFileFolderPath, out string zipFilePath)
        {
            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            string logFilePath = Path.Combine(rootPath, "KSP.log");
            string mmCacheFilePath = Path.Combine(rootPath, "GameData", "ModuleManager.ConfigCache");
            zipFileFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrEmpty(zipFileFolderPath))
            {
                zipFileFolderPath = rootPath;
            }

            zipFileFolderPath = Path.Combine(zipFileFolderPath, "KSPBugReports");

            try
            {
                Directory.CreateDirectory(zipFileFolderPath);
            }
            catch (Exception e)
            {
                Lib.Log($"Could not create directory {zipFileFolderPath}\n{e}", Lib.LogLevel.Warning);
                zipFileName = zipFilePath = string.Empty;
                return false;
            }

            string fileName = "KSPBugReport_" + DateTime.Now.ToString(@"yyyy-MM-dd_HHmmss");
            zipFileName = fileName + ".zip";
            zipFilePath = Path.Combine(zipFileFolderPath, zipFileName);

            Lib.Log($"Creating bug report : {zipFilePath}");

            using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                try
                {
                    if (File.Exists(logFilePath))
                    {
                        ForceFlushKspLogToDisk();
                        using (Stream fileStream = File.Open(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            ZipArchiveEntry zipArchiveEntry = archive.CreateEntry("KSP.log", System.IO.Compression.CompressionLevel.Optimal);
                            using (Stream entryStream = zipArchiveEntry.Open())
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Lib.Log($"Could not zip KSP.log\n{e}", Lib.LogLevel.Warning);
                    archive.Dispose();
                    return false;
                }

                string kspLogsPath = Path.Combine(rootPath, "Logs");
                string kopernicusLogsPath = Path.Combine(kspLogsPath, "Kopernicus");
                if (Directory.Exists(kopernicusLogsPath))
                {
                    try
                    {
                        string[] kopernicusLogs = Directory.GetFiles(kopernicusLogsPath, "*.log", SearchOption.AllDirectories);

                        DateTime startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime;
                        foreach (string kopernicusLog in kopernicusLogs)
                        {
                            using (Stream fileStream = File.Open(kopernicusLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                string entryPath = kopernicusLog.Replace(kspLogsPath, string.Empty).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                if (File.GetLastWriteTime(kopernicusLog) < startTime)
                                {
                                    entryPath += ".oldsession";
                                }
                                ZipArchiveEntry zipArchiveEntry = archive.CreateEntry(entryPath, System.IO.Compression.CompressionLevel.Optimal);
                                using (Stream entryStream = zipArchiveEntry.Open())
                                {
                                    fileStream.CopyTo(entryStream);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Lib.Log($"Error writing Kopernicus logs\n{e}", Lib.LogLevel.Warning);
                    }
                }

                try
                {
                    if (File.Exists(mmCacheFilePath))
                    {
                        using (Stream fileStream = File.Open(mmCacheFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            ZipArchiveEntry zipArchiveEntry = archive.CreateEntry("ModuleManager.ConfigCache", System.IO.Compression.CompressionLevel.Optimal);
                            using (Stream entryStream = zipArchiveEntry.Open())
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                    }
                    else
                    {
                        ZipArchiveEntry zipArchiveEntry = archive.CreateEntry("Configs.txt", System.IO.Compression.CompressionLevel.Optimal);
                        using (Stream entryStream = zipArchiveEntry.Open())
                        using (StreamWriter writer = new StreamWriter(entryStream))
                        {
                            foreach (UrlDir.UrlConfig urlConfig in GameDatabase.Instance.root.AllConfigs)
                            {
                                writer.Write("Config : \"");
                                writer.Write(urlConfig.url);
                                writer.Write("\"\n");
                                writer.Write(urlConfig.config);
                                writer.Write("\n");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Lib.Log($"Could not get configs from ModuleManager.ConfigCache or the game database\n{e}", Lib.LogLevel.Warning);
                }

                try
                {
                    if (HighLogic.CurrentGame != null && Lib.IsGameRunning)
                    {
                        Game game = HighLogic.CurrentGame.Updated();
                        game.startScene = GameScenes.SPACECENTER;
                        ConfigNode configNode = new ConfigNode();
                        game.Save(configNode);
                        configNode = configNode.nodes[0];

                        ZipArchiveEntry zipArchiveEntry = archive.CreateEntry(HighLogic.SaveFolder + ".sfs", System.IO.Compression.CompressionLevel.Optimal);
                        using (Stream entryStream = zipArchiveEntry.Open())
                        {
                            using (StreamWriter writer = new StreamWriter(entryStream))
                            {
                                writer.Write(configNode.ToString());
                                writer.Flush();
                            }
                        }
                    }
                    else
                    {
                        Lib.Log($"Could not add savegame to bug report, no save loaded or saving not available in current scene");
                    }
                }
                catch (Exception e)
                {
                    Lib.Log($"Could not zip savegame\n{e}", Lib.LogLevel.Warning);
                }
            }
            return true;
        }

        private static IEnumerator TakeScreenshot()
        {
            DebugScreen instance = FindObjectOfType<DebugScreen>();
            if (instance != null)
            {
                yield return null; // prevent error in DebugScreen
                instance.Hide();
                yield return null; // make sure the debug screen is hidden when we take the screenshot
            }

            yield return new WaitForEndOfFrame(); // necessary so everything is rendered as it should

            try
            {
                // Doesn't work as of KSP 1.11 / DX11 : ground texture are rendered as transparent for some reason
                // byte[] shotPNG = ScreenCapture.CaptureScreenshotAsTexture(1).EncodeToPNG();

                // Create a texture the size of the screen, RGB24 format
                int width = Screen.width;
                int height = Screen.height;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

                // Read screen contents into the texture
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                byte[] shotPNG = tex.EncodeToPNG();

                
                string pngFileName = null;

                using (ZipArchive archive = ZipFile.Open(lastReportPath, ZipArchiveMode.Update))
                {
                    using (Stream memoryStream = new MemoryStream(shotPNG))
                    {
                        pngFileName = "Screenshot_" + DateTime.Now.ToString(@"yyyy-MM-dd_HHmmss") + ".png";
                        ZipArchiveEntry zipArchiveEntry =
                            archive.CreateEntry(pngFileName, System.IO.Compression.CompressionLevel.Optimal);
                        using (Stream entryStream = zipArchiveEntry.Open())
                        {
                            memoryStream.CopyTo(entryStream);
                        }
                    }
                }

                ScreenMessages.PostScreenMessage(
                    $"Screenshot :\n<color=#FF8000>{pngFileName}</color>\n added to report :\n<color=#FF8000>{Path.GetFileName(lastReportPath)}</color>",
                    5f, ScreenMessageStyle.UPPER_CENTER, true);
                Lib.Log($"Screenshot {pngFileName} added to report {Path.GetFileName(lastReportPath)}");
            }
            catch (Exception e)
            {
                ScreenMessages.PostScreenMessage($"Error creating a screenshot", 5f, ScreenMessageStyle.UPPER_CENTER, Color.red);
                Lib.Log($"Error creating screenshot\n{e}", Lib.LogLevel.Warning);
            }

            if (instance != null)
            {
                yield return null;
                instance.Show();
            }
        }

        public static void ForceFlushKspLogToDisk()
        {
            if (GameSettings.LOG_INSTANT_FLUSH)
                return;

            GameSettings.LOG_INSTANT_FLUSH = true;
            Lib.Log("Flushing KSP.log to disk...");
            GameSettings.LOG_INSTANT_FLUSH = false;
        }

        #endregion
    }
}
