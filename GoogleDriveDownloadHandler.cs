using System;
using System.ComponentModel;
using System.Net;

namespace Polynurm_Launcher
{
    public class GoogleDriveDownloadHandler
    {
        public event AsyncCompletedEventHandler DownloadFileCompleted;

        public void BeginDownload(string address, string filePath)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFileCompleted += FileDownloadedCallback;
            address = CreateDirectGoogleDriveLink(address);
            webClient.DownloadFileAsync(new Uri(address), filePath);
        }

        public void FileDownloadedCallback (object sender, AsyncCompletedEventArgs e)
        {
            DownloadFileCompleted(this, e);
        }

        //Link has to be in form of: drive.google.com/file/d/(file-url)/view
        private string CreateDirectGoogleDriveLink (string address)
        {
            return address.Replace("file/d/", "uc?id=").Replace("/view", "&export=download&confirm=t");
        }
    }
}
