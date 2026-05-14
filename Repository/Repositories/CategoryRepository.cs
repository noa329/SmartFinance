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
    public class CategoryRepository: IRepository<Category>
    {
        private readonly IContext ctx;

        public CategoryRepository(IContext context)
        {
            ctx = context;
        }

        public async Task<Category> Add(Category entity)
        {
            await ctx.Categories.AddAsync(entity);
            await ctx.Save();
            return entity;
        }

        public async Task Delete(int id)
        {
            var category = await GetById(id);
            if (category != null)
            {
                ctx.Categories.Remove(category);
                await ctx.Save();
            }
        }

        public async Task<List<Category>> Find(System.Linq.Expressions.Expression<Func<Category, bool>> predicate)
        {
            return await ctx.Categories.Where(predicate).ToListAsync();
        }

        public async Task<List<Category>> GetAll()
        {
            return await ctx.Categories.ToListAsync();

        }

        public async Task<Category?> GetById(int id)
        {
            return await ctx.Categories.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Category> Update(int id,Category entity)
        {
            var existingCategory = await GetById(id);
           if (existingCategory == null)
                throw new KeyNotFoundException("Category not found");

            existingCategory.Name = entity.Name;
            existingCategory.IsFixed = entity.IsFixed;
            existingCategory.BudgetLimit= entity.BudgetLimit;

            await ctx.Save();
            return existingCategory;
          
        }

    }
}
