using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQM.Core.Entities
{
    public class FTPSetting
    {
        [Key]
        public int Id { get; set; }
        public required string FtpHost { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public string? RootFolderName { get; set; }
    }
}
