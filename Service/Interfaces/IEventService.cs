using Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IEventService
    {
        Task<List<EventDto>> GetUserEvents(int userId);
        Task<EventDto> GetEventStats(int eventId, int userId);

        Task AddEvent(EventCreateDto dto, int userId);
        Task UpdateEvent(int id, EventUpdateDto dto, int userId);
        Task DeleteEvent(int id, int userId);
    }
}
