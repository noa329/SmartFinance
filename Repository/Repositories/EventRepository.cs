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
    public class EventRepository:IRepository<Event>
    {
        private readonly IContext ctx;

        public EventRepository(IContext context)
        {
            ctx = context;
        }

        public async Task<Event> Add(Event entity)
        {
            await ctx.Events.AddAsync(entity);
            await ctx.Save();
            return entity;
        }

        public async Task Delete(int id)
        {
            var events = await GetById(id);
            if (events != null)
            {
                ctx.Events.Remove(events);
                await ctx.Save();
            }
        }

        public async Task<List<Event>> Find(System.Linq.Expressions.Expression<Func<Event, bool>> predicate)
        {
            return await ctx.Events.Where(predicate).ToListAsync();
        }

        public async Task<List<Event>> GetAll()
        {
            return await ctx.Events.ToListAsync();
        }

        public async Task<Event?> GetById(int id)
        {
            return await ctx.Events.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Event> Update(int id, Event entity)
        {
            var existingEvent = await GetById(id);
            if (existingEvent == null)
                throw new KeyNotFoundException("Event not found");

            existingEvent.UserId = entity.UserId;
            existingEvent.StartDate = entity.StartDate;
            existingEvent.EndDate = entity.EndDate;
            existingEvent.Name = entity.Name;
            existingEvent.Budget = entity.Budget;

            await ctx.Save();
            return existingEvent;
            
        }
    }
}
