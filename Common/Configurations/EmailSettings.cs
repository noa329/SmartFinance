using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Configurations
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public int Port { get; set; }
        public string SenderPassword { get; set; } = string.Empty;
    }
}
