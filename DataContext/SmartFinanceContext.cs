using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DataContext
{
    public class SmartFinanceContext : DbContext
    {
        public SmartFinanceContext(DbContextOptions<SmartFinanceContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Income> Incomes { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<User>().HasMany(u => u.Expenses).WithOne(e => e.User).HasForeignKey(e => e.UserId);
            modelBuilder.Entity<User>().HasMany(u => u.Events).WithOne(ev => ev.User).HasForeignKey(ev => ev.UserId);
            modelBuilder.Entity<User>().HasMany(u => u.Incomes).WithOne(i => i.User).HasForeignKey(i => i.UserId);
            modelBuilder.Entity<User>().HasMany(u => u.Budgets).WithOne(b => b.User).HasForeignKey(b => b.UserId);

            modelBuilder.Entity<Category>().HasMany(c => c.Expenses).WithOne(e => e.Category).HasForeignKey(e => e.CategoryId);
            modelBuilder.Entity<Category>().HasMany(c => c.Budgets).WithOne(b => b.Category).HasForeignKey(b => b.CategoryId);

            modelBuilder.Entity<Event>().HasMany(ev => ev.Expenses).WithOne(e => e.Event).HasForeignKey(e => e.EventId);

            modelBuilder.Entity<Alert>().HasOne(a => a.User).WithMany(u => u.Alerts).HasForeignKey(a => a.UserId);

        }
    }
}
