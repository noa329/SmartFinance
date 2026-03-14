using AutoMapper;
using Common.Dto;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Sevices
{
    public class EventService : IEventService
    {
        private readonly IRepository<Event> _eventRepo;
        private readonly IRepository<Expense> _expenseRepo;
        private readonly IMapper _mapper;

        public EventService(IRepository<Event> eventRepo,
              IRepository<Expense> expenseRepo,IMapper mapper)
        {
            _eventRepo = eventRepo;
            _expenseRepo = expenseRepo;
            _mapper = mapper;
        }

        public async Task<List<EventDto>> GetUserEvents(int userId)
        {
            var events = await _eventRepo
                .Find(e => e.UserId == userId);

            var list = new List<EventDto>();

            foreach (var ev in events)
            {
                var dto = await GetEventStats(ev.Id, userId);
                list.Add(dto);
            }

            return list;
        }

        public async Task<EventDto> GetEventStats(int eventId, int userId)
        {
            var ev = await _eventRepo.GetById(eventId);

            if (ev == null || ev.UserId != userId)
                throw new Exception("Unauthorized");

            var expenses = await _expenseRepo
                .Find(e => e.EventId == eventId);

            var dto = _mapper.Map<EventDto>(ev);

            dto.Expenses =
                _mapper.Map<List<ExpenseDto>>(expenses);

            dto.TotalSpent = expenses.Sum(e => e.TotalAmount);

            dto.RemainingBudget = ev.Budget - dto.TotalSpent;

            dto.Progress =
                ev.Budget == 0 ? 0 :
                (double)(dto.TotalSpent / ev.Budget) * 100;

            return dto;
        }

        public async Task AddEvent(EventCreateDto dto, int userId)
        {
            var entity = _mapper.Map<Event>(dto);

            entity.UserId = userId;

            await _eventRepo.Add(entity);
        }
        public async Task UpdateEvent(int id, EventUpdateDto dto, int userId)
        {
            var entity = await _eventRepo.GetById(id);

            if (entity == null || entity.UserId != userId)
                throw new Exception("Unauthorized");

            _mapper.Map(dto, entity);

            await _eventRepo.Update(id, entity);
        }
        public async Task DeleteEvent(int id, int userId)
        {
            var entity = await _eventRepo.GetById(id);

            if (entity == null || entity.UserId != userId)
                throw new Exception("Not found");

            if (entity.UserId != userId)
                throw new Exception("Unauthorized");

            await _eventRepo.Delete(id);
        }
    }
}
