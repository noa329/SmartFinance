using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace Repository.Repositories
{
    public class AlertRepository:IRepository<Alert>
    {
        private readonly IContext ctx;

        public AlertRepository(IContext context)
        {
            ctx = context;
        }

        public async Task<Alert> Add(Alert entity)
        {
            await ctx.Alerts.AddAsync(entity);
            await ctx.Save();
            return entity;
        }

        public async Task Delete(int id)
        {
            var alert = await GetById(id);
            if (alert != null)
            {
                ctx.Alerts.Remove(alert);
                await ctx.Save();
            }
        }

        public async Task<List<Alert>> Find(System.Linq.Expressions.Expression<Func<Alert, bool>> predicate)
        {
            return await ctx.Alerts.Where(predicate).ToListAsync();
        }

        public async Task<List<Alert>> GetAll()
        {
            return await ctx.Alerts.ToListAsync();
        }

        public async Task<Alert?> GetById(int id)
        {
            return await ctx.Alerts.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Alert> Update(int id,Alert entity)
        {
           var existingAlert = await GetById(id);
            if (existingAlert == null)
                throw new KeyNotFoundException("Alert not found");

            existingAlert.UserId = entity.UserId;
            existingAlert.Type = entity.Type;
            existingAlert.Title = entity.Title;
            existingAlert.Message = entity.Message;
            existingAlert.IsRead = entity.IsRead;
            existingAlert.ReadAt = entity.ReadAt;
            existingAlert.RelatedEntityType = entity.RelatedEntityType;
            existingAlert.RelatedEntityId = entity.RelatedEntityId;

            await ctx.Save();
            return existingAlert;
          
        }

    }
}
