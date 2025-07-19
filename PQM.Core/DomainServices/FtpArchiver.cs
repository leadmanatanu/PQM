using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.DomainServices
{
    public class FtpArchiver
    {
        private readonly string ftpHost;
        private readonly string username;
        private readonly string password;

        public FtpArchiver(string ftpHost, string username, string password)
        {
            this.ftpHost = ftpHost;
            this.username = username;
            this.password = password;
        }

        public void ArchiveFile(string sourcePath, string archivePath)
        {
            string sourceUrl = $"{ftpHost}/{sourcePath}";
            string destinationUrl = $"{ftpHost}/{archivePath}";

            // Move file by renaming it
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(sourceUrl);
            request.Method = WebRequestMethods.Ftp.Rename;
            request.RenameTo = archivePath;
            request.Credentials = new NetworkCredential(username, password);
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = false;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"File archived: {sourcePath} to {archivePath}. Status: {response.StatusDescription}");
            }
        }
    }
}
