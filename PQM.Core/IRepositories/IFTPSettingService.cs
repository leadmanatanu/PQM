using PQM.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.IRepositories
{
    public interface IFTPSettingService
    {
        bool AddUpdateFTP(FTPSetting ftpSetting);
        FTPSetting GetFTPSetting();
    }
}
