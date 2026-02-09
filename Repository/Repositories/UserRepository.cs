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
    public class UserRepository : IRepository<User>
    {
        private readonly IContext ctx;

        public UserRepository(IContext context)
        {
            ctx = context;
        }

        public async Task<User> Add(User entity)
        {
            await ctx.Users.AddAsync(entity);
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

        public async Task<List<User>> Find(System.Linq.Expressions.Expression<Func<User, bool>> predicate)
        {
            return await ctx.Users.Where(predicate).ToListAsync();
        }

        public async Task<List<User>> GetAll()
        {
            return await ctx.Users.ToListAsync();

        }

        public async Task<User?> GetById(int id)
        {
            return await ctx.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User> Update(User entity)
        {
            var existingUser = await GetById(entity.Id);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");
            
            existingUser.FullName = entity.FullName;
            existingUser.Email = entity.Email;
            existingUser.PhoneNumber = entity.PhoneNumber;
            existingUser.LastLoginAt = entity.LastLoginAt;
            existingUser.PasswordHash = entity.PasswordHash;
            existingUser.Role = entity.Role;

            await ctx.Save();
            return existingUser;
        }

    }
}