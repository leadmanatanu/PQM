using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.IRepositories
{
    public interface ISFTPService
    {
        List<string> GetFiles(string url, string userName, string password, string ftpFolder, string localFolder);
    }
}
