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
            var user = await GetById(id);
            if (user != null)
            {
                ctx.Users.Remove(user);
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
            ctx.Expenses.Update(entity);
            await ctx.Save();
            return entity;
        }


    }
}
