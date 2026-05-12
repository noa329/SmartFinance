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
        private readonly IContext _ctx;

        public BudgetRepository(IContext context)
        {
            _ctx = context;
        }

        public async Task<Budget> Add(Budget entity)
        {
            await _ctx.Budgets.AddAsync(entity);
            await _ctx.Save();
            return entity;
        }

        public async Task Delete(int id)
        {
            var budget = await GetById(id);
            if (budget != null)
            {
                _ctx.Budgets.Remove(budget);
                await _ctx.Save();
            }
        }

        public async Task<List<Budget>> Find(System.Linq.Expressions.Expression<Func<Budget, bool>> predicate)
        {
           return await _ctx.Budgets.Include(b => b.Category).Where(predicate).ToListAsync();
        }

        public async Task<List<Budget>> GetAll()
        {
            return await _ctx.Budgets.ToListAsync();
        }

        public async Task<Budget?> GetById(int id)
        {
            return await _ctx.Budgets.Include(b => b.Category).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Budget> Update(int id,Budget entity)
        {
            var existingBudget = await GetById(id);
            if (existingBudget == null)
                throw new KeyNotFoundException("Budget not found");

            existingBudget.UserId = entity.UserId;
            existingBudget.CategoryId = entity.CategoryId;
            existingBudget.Percentage = entity.Percentage;
            existingBudget.AlertEnabled = entity.AlertEnabled;
            existingBudget.AlertThreshold = entity.AlertThreshold;

            await _ctx.Save();
            return existingBudget;
            
        }
       

    }
}
