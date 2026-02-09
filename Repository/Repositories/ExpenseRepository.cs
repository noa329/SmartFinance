using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class ExpenseRepository:IRepository<Expense>
    {
        private readonly IContext ctx;

        public ExpenseRepository(IContext context) 
        {
            ctx = context;
        }

        public async Task<Expense> Add(Expense entity)
        {
            await ctx.Expenses.AddAsync(entity);
            await ctx.Save();
            return entity;
        }

        public async Task Delete(int id)
        {
            var expense = await GetById(id);
            if (expense != null)
            {
                ctx.Expenses.Remove(expense);
                await ctx.Save();
            }
        }

        public async Task<List<Expense>> Find(System.Linq.Expressions.Expression<Func<Expense, bool>> predicate)
        {
            return await ctx.Expenses.Where(predicate).ToListAsync();
        }

        public async Task<List<Expense>> GetAll()
        {
            return await ctx.Expenses.ToListAsync();
        }

        public async Task<Expense?> GetById(int id)
        {
            return await ctx.Expenses.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Expense> Update(Expense entity)
        {
           var existingExpense = await GetById(entity.Id);
            if (existingExpense == null)
                throw new KeyNotFoundException("Expense not found");

            existingExpense.UserId = entity.UserId;
            existingExpense.CategoryId = entity.CategoryId;
            existingExpense.MerchantName = entity.MerchantName;
            existingExpense.TotalAmount = entity.TotalAmount;
            existingExpense.TransactionDate = entity.TransactionDate;
            existingExpense.EventId = entity.EventId;
            existingExpense.InstallmentsCount = entity.InstallmentsCount;
            existingExpense.InstallmentAmount = entity.InstallmentAmount;
            existingExpense.FirstInstallmentDate = entity.FirstInstallmentDate;
            existingExpense.Source = entity.Source;
            existingExpense.OriginalText = entity.OriginalText;
           
            await ctx.Save();
            return existingExpense;
           
        }


    }
}
