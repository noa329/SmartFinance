using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IEmailService
    {
        Task SendBudgetAlert(string toEmail, string category, double spent, double limit);

    }
}
