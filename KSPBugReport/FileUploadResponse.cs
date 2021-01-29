using System;

namespace KSPBugReport
{
    [Serializable]
    public class FileUploadResponse_Fileio
    {
        // {"success":true,"key":"2ojE41","link":"https://file.io/2ojE41","expiry":"14 days"}
        public bool sucess;
        public string key;
        public string link;
        public string expiry;
    }
}
