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
    public class IncomeRepository:IRepository<Income>
    {
        private readonly IContext ctx;

        public IncomeRepository(IContext context)
        {
            ctx = context;
        }

        public async Task<Income> Add(Income entity)
        {
            await ctx.Incomes.AddAsync(entity);
            await ctx.Save();
            return entity;
        }

        public async Task Delete(int id)
        {
            var income = await GetById(id);
            if (income != null)
            {
                ctx.Incomes.Remove(income);
                await ctx.Save();
            }
        }

        public async Task<List<Income>> Find(System.Linq.Expressions.Expression<Func<Income, bool>> predicate)
        {
            return await ctx.Incomes.Where(predicate).ToListAsync();
        }

        public async Task<List<Income>> GetAll()
        {
            return await ctx.Incomes.ToListAsync();
        }

        public async Task<Income?> GetById(int id)
        {
            return await ctx.Incomes.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Income> Update(int id, Income entity)
        {
            var existingIncome = await GetById(id);
            if (existingIncome == null)
                throw new KeyNotFoundException("Income not found");

            existingIncome.UserId = entity.UserId;
            existingIncome.Amount = entity.Amount;
            existingIncome.Date = entity.Date;
            existingIncome.IsRecurring = entity.IsRecurring;
            existingIncome.Source = entity.Source;

            await ctx.Save();
            return existingIncome;
        }

    }
}
