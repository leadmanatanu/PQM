using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PQM.Core.Entities;
using PQM.Core.IRepositories;
using System.ComponentModel.DataAnnotations.Schema;

namespace PQM.Infrastructure.Repositories
{
    public class FTPSettingService : IFTPSettingService
    {
        public string _connectionString { get; set; }

        public FTPSettingService(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public bool AddUpdateFTP(FTPSetting ftpSetting)
        {
            DataContext dbContext = new DataContext(this._connectionString);
            if (ftpSetting.Id == 0)
            {
                dbContext.FTPSetting.Add(ftpSetting);
            }
            else
            {
                var ftpData = dbContext.FTPSetting.FirstOrDefault(x => x.Id == ftpSetting.Id);
                if (ftpData == null)
                {
                    return false;
                }
                ftpData.FtpHost = ftpSetting.FtpHost;
                ftpData.UserName = ftpSetting.UserName;
                ftpData.Password = ftpSetting.Password;
                ftpData.RootFolderName = ftpSetting.RootFolderName;
                dbContext.FTPSetting.Update(ftpData);
            }
            dbContext.SaveChanges();
            return true;
        }

        public FTPSetting? GetFTPSetting()
        {
            DataContext dbContext = new DataContext(this._connectionString);
            return dbContext.FTPSetting.OrderByDescending(x => x.Id).FirstOrDefault();
        }

    }
}
