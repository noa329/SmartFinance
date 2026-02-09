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
    public class UserRepository :IRepository<User>, IUserRepository
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

        public  async Task<List<User>> GetAll()
        {
            return await ctx.Users
                .Select(u => new User
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    Role = u.Role,
                    PasswordHash = "" 
                })
                .ToListAsync();
        }

        public async Task<User?> GetById(int id)
        {
            return await ctx.Users.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User> Update(User entity)
        {
            var existingUser = await GetById(entity.Id);
            if (existingUser == null)
            {
                throw new Exception("User not found");
            }
            existingUser.FullName = entity.FullName;
            existingUser.Email = entity.Email;
            existingUser.PhoneNumber = entity.PhoneNumber;
            existingUser.LastLoginAt = entity.LastLoginAt;
            existingUser.PasswordHash = entity.PasswordHash;
            existingUser.Role = entity.Role;
            ctx.Users.Update(existingUser);
            await ctx.Save();
            return existingUser;
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await ctx.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByPhoneNumber(string phoneNumber)
        {
            return await ctx.Users.FirstOrDefaultAsync(x=>x.PhoneNumber == phoneNumber);
        }

        public async Task<bool> EmailExists(string email)
        {
            return await ctx.Users.AnyAsync(x=> x.Email == email);
        }


        public async Task<User?> GetUserWithExpenses(int userId)
        {
            return await ctx.Users.Include(u => u.Expenses)
                    .ThenInclude(e => e.Category)
                .Include(u => u.Expenses)
                    .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithBudgets(int userId)
        {
            return await ctx.Users
                .Include(u => u.Budgets)
                    .ThenInclude(b => b.Category)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithIncomes(int userId)
        {
            return await ctx.Users
                .Include(u => u.Incomes)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithAlerts(int userId)
        {
            return await ctx.Users
                .Include(u => u.Alerts.OrderByDescending(a => a.CreatedAt))
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithEvents(int userId)
        {
            return await ctx.Users
                .Include(u => u.Events)
                    .ThenInclude(e => e.Expenses)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithAllData(int userId)
        {
            return await ctx.Users
                .Include(u => u.Expenses)
                    .ThenInclude(e => e.Category)
                .Include(u => u.Expenses)
                    .ThenInclude(e => e.Event)
                .Include(u => u.Budgets)
                    .ThenInclude(b => b.Category)
                .Include(u => u.Incomes)
                .Include(u => u.Events)
                .Include(u => u.Alerts)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateLastLogin(int userId)
        {
            var user = await GetById(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await Update(user);
            }
        }


    }
}
