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
        private static string lastReportUploadURI = string.Empty;

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
            // list of potential services here : https://gist.github.com/Prajjwal/2226c6a96d1d72abc713e889160a9f81

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", File.ReadAllBytes(lastReportPath), Path.GetFileName(lastReportPath));

            using (UnityWebRequest www = UnityWebRequest.Post("https://0x0.st", form))
            {
                yield return www.SendWebRequest();

                try
                {
                    if (www.isNetworkError || www.isHttpError)
                    {
                        Lib.Log(www.error, Lib.LogLevel.Warning);
                        throw new Exception();
                    }
                    else
                    {
                        lastReportUploadURI = www.downloadHandler.text;
                        ScreenMessages.PostScreenMessage(
                            $"Upload complete for <color=#FF8000>{Path.GetFileName(lastReportPath)}</color> to the 0x0.st sharing service (~1 year retention)",
                            10f, ScreenMessageStyle.UPPER_CENTER, true);
                        CopyLastLinkToClipboard();
                        yield break;
                    }
                }
                catch (Exception)
                {
                    ScreenMessages.PostScreenMessage($"Upload failed to 0x0.st", 3f, ScreenMessageStyle.UPPER_CENTER, Color.red);
                }
            }

            using (UnityWebRequest www = UnityWebRequest.Post("https://file.io", form))
            {
                yield return www.SendWebRequest();

                try
                {
                    if (www.isNetworkError || www.isHttpError)
                    {
                        Lib.Log(www.error, Lib.LogLevel.Warning);
                        throw new Exception();
                    }
                    else
                    {
                        FileUploadResponse_Fileio response = JsonUtility.FromJson<FileUploadResponse_Fileio>(www.downloadHandler.text);
                        lastReportUploadURI = response.link;
                        ScreenMessages.PostScreenMessage(
                            $"Upload complete for <color=#FF8000>{Path.GetFileName(lastReportPath)}</color> to the file.io sharing service (file deleted after first download, 14 days retention)"
                            , 10f, ScreenMessageStyle.UPPER_CENTER, true);
                        CopyLastLinkToClipboard();
                        yield break;
                    }
                }
                catch (Exception)
                {
                    ScreenMessages.PostScreenMessage($"Upload failed to file.io", 3f, ScreenMessageStyle.UPPER_CENTER, Color.red);
                }
            }
        }

        private static void CopyLastLinkToClipboard()
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

        #endregion

        #region BUG REPORT CREATION

        private static bool CreateReport(out string zipFileName, out string zipFileFolderPath, out string zipFilePath)
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KSP.log");
            string mmCacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameData", "ModuleManager.ConfigCache");
            zipFileFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrEmpty(zipFileFolderPath))
            {
                zipFileFolderPath = AppDomain.CurrentDomain.BaseDirectory;
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
