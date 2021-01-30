using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace KSPBugReport
{
    // list of potential services here : https://gist.github.com/Prajjwal/2226c6a96d1d72abc713e889160a9f81

    public abstract class UploadService
    {
        protected string serviceURL;
        protected string serviceName;
        protected string serviceComment;
        protected byte[] file;
        protected string filename;
        protected WWWForm form;
        protected bool success = false;
        public bool Success => success;

        protected UploadService(byte[] file, string filename)
        {
            this.file = file;
            this.filename = filename;
        }

        public IEnumerator Send()
        {
            using (UnityWebRequest www = UnityWebRequest.Post(serviceURL, form))
            {
                www.SendWebRequest();

                ScreenMessage uploadMessage = null;
                while (!www.isDone)
                {
                    if (uploadMessage == null || !ScreenMessages.Instance.ActiveMessages.Contains(uploadMessage))
                    {
                        uploadMessage = ScreenMessages.PostScreenMessage(
                            $"Uploading to {serviceName} : {www.uploadProgress:P0}", 1f,
                            ScreenMessageStyle.UPPER_CENTER);
                    }

                    yield return null;
                }

                try
                {
                    if (www.isNetworkError || www.isHttpError)
                    {
                        Lib.Log(www.error, Lib.LogLevel.Warning);
                        throw new Exception();
                    }
                    else
                    {
                        if (!EvalResult(www.downloadHandler.text, out KSPBugReport.lastReportUploadURI))
                        {
                            throw new Exception("Upload failed : bad response");
                        }
                        ScreenMessages.PostScreenMessage(
                            $"Upload complete for <color=#FF8000>{filename}</color> to {serviceName}\n({serviceComment})"
                            , 10f, ScreenMessageStyle.UPPER_CENTER, true);
                        KSPBugReport.CopyLastLinkToClipboard();
                        success = true;
                    }
                }
                catch (Exception e)
                {
                    ScreenMessages.PostScreenMessage($"Upload failed to {serviceName}", 5f,
                        ScreenMessageStyle.UPPER_CENTER, Color.red);
                    Lib.Log($"Upload failed to {serviceName}\n{e}", Lib.LogLevel.Warning);
                }
            }
        }

        protected abstract bool EvalResult(string result, out string downloadURL);
    }

    public class UploadService0x0 : UploadService
    {
        public UploadService0x0(byte[] file, string filename) : base(file, filename)
        {
            serviceName = "0x0.st";
            serviceURL = "https://0x0.st";
            serviceComment = "30 days min retention";

            form = new WWWForm();
            form.AddBinaryData("file", file, filename);
        }

        protected override bool EvalResult(string result, out string downloadURL)
        {
            downloadURL = result;
            return !string.IsNullOrEmpty(downloadURL);
        }
    }

    public class UploadServiceOshi : UploadService
    {
        public UploadServiceOshi(byte[] file, string filename) : base(file, filename)
        {
            serviceName = "oshi.at";
            serviceURL = "https://oshi.at";
            serviceComment = "90 days retention";

            form = new WWWForm();
            form.AddField("expire", 129600); // 90 days (max possible)
            form.AddBinaryData("f", file, filename);
        }

        protected override bool EvalResult(string result, out string downloadURL)
        {
            // result example :
            // "MANAGE: https://oshi.at/a/d170436da3614bf0a9a9c12e519d91292adf10e7\nDL: https://oshi.at/FSmYHq/KSPBugReport_2021-01-30_170757.zip\n"

            if (string.IsNullOrEmpty(result) || !result.Contains("DL:"))
            {
                downloadURL = string.Empty;
                return false;
            }

            downloadURL = result.Substring(result.IndexOf("DL:") + 3).Trim(' ', '\n');
            return !string.IsNullOrEmpty(downloadURL);
        }
    }

    public class UploadServiceFileio : UploadService
    {
        public UploadServiceFileio(byte[] file, string filename) : base(file, filename)
        {
            serviceName = "file.io";
            serviceURL = "https://file.io";
            serviceComment = "file deleted after first download, 14 days retention";

            form = new WWWForm();
            form.AddBinaryData("file", file, filename);
        }

        [Serializable]
        private class FileUploadResponse
        {
            // {"success":true,"key":"2ojE41","link":"https://file.io/2ojE41","expiry":"14 days"}
            public bool success;
            public string key;
            public string link;
            public string expiry;
        }

        protected override bool EvalResult(string result, out string downloadURL)
        {
            FileUploadResponse response = JsonUtility.FromJson<FileUploadResponse>(result);

            if (response.success)
            {
                downloadURL = response.link;
                return true;
            }

            downloadURL = string.Empty;
            return false;
        }
    }
}
