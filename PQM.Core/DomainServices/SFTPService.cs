using PQM.Core.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PQM.Core.DomainServices
{
    public class SFTPService : ISFTPService
    {
        public List<string> GetFiles(string url, string userName, string password, string ftpFolder, string localFolder)
        {
            string sftpUrl = url + ftpFolder + "/";
            var csvFiles = ListCsvFilesFromFtp(sftpUrl, userName, password);
            foreach (var file in csvFiles)
            {
                DownloadFileFromFtp(sftpUrl, userName, password, file, localFolder);
            }
            return csvFiles;
        }

        static List<string> ListCsvFilesFromFtp(string ftpUrl, string ftpUser, string ftpPassword)
        {
            var files = new List<string>();
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(ftpUser, ftpPassword);

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                while (!reader.EndOfStream)
                {
                    var fileName = reader.ReadLine();
                    if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        files.Add(fileName);
                    }
                }
            }

            return files;
        }

        static void DownloadFileFromFtp(string ftpUrl, string ftpUser, string ftpPassword, string fileName, string localFolder)
        {
            string remoteFile = ftpUrl + fileName;
            string localFile = Path.Combine(localFolder, fileName);

            //Directory.CreateDirectory("DownloadedFiles");

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remoteFile);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(ftpUser, ftpPassword);

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (FileStream outputStream = new FileStream(localFile, FileMode.Create))
            {
                responseStream.CopyTo(outputStream);
            }
            //Console.WriteLine($"Downloaded: {fileName}");


            // Remove file from ftp
            DeleteFileFromFtp(ftpUrl, ftpUser, ftpPassword, fileName);
        }


        static void DeleteFileFromFtp(string ftpUrl, string ftpUser, string ftpPassword, string fileName)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl + "/" + fileName);
            request.Method = WebRequestMethods.Ftp.DeleteFile;

            request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = false;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                //Console.WriteLine($"Delete status: {response.StatusDescription}");
            }
        }
    }
}
