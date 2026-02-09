using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IContext
    {
        DbSet<User> Users { get; set; }
        DbSet<Expense> Expenses { get; set; }
        DbSet<Category> Categories { get; set; }
        DbSet<Event> Events { get; set; }
        DbSet<Income> Incomes { get; set; }
        DbSet<Budget> Budgets { get; set; }
        DbSet<Alert> Alerts { get; set; }

        DbSet<T> Set<T>() where T : class;

        Task Save();
    }
}
