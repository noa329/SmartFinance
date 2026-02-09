using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Repository.Repositories
{
    public class BudgetRepository: IRepository<Budget>
    {
        private readonly IContext ctx;

        public BudgetRepository(IContext context)
        {
            ctx = context;
        }

        public async Task<Budget> Add(Budget entity)
        {
            await ctx.Budgets.AddAsync(entity);
            await ctx.Save();
            return entity;
        }

        public async Task Delete(int id)
        {
            var budget = await GetById(id);
            if (budget != null)
            {
                ctx.Budgets.Remove(budget);
                await ctx.Save();
            }
        }

        public async Task<List<Budget>> Find(System.Linq.Expressions.Expression<Func<Budget, bool>> predicate)
        {
            return await ctx.Budgets.Where(predicate).ToListAsync();
        }

        public async Task<List<Budget>> GetAll()
        {
            return await ctx.Budgets.ToListAsync();
        }

        public async Task<Budget?> GetById(int id)
        {
            return await ctx.Budgets.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Budget> Update(Budget entity)
        {
            var existingBudget = await GetById(entity.Id);
            if (existingBudget == null)
                throw new KeyNotFoundException("Budget not found");

            existingBudget.UserId = entity.UserId;
            existingBudget.CategoryId = entity.CategoryId;
            existingBudget.Percentage = entity.Percentage;
            existingBudget.AlertEnabled = entity.AlertEnabled;
            existingBudget.AlertThreshold = entity.AlertThreshold;

            await ctx.Save();
            return existingBudget;
            
        }
       

    }
}
