using AutoMapper;
using Common.Dto;
using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;
using Service.Interfaces;

namespace Service.Services
{
    public class AlertService : IAlertService
    {
        private readonly IRepository<Alert> _alertRepository;
        private readonly IMapper _mapper;

        public AlertService(
            IRepository<Alert> repository,
            IMapper mapper )
           
        {
            _alertRepository = repository;
            _mapper = mapper;
        }

        //public async Task<List<AlertDto>> GetUserAlerts(int userId)
        //{
        //    var alerts = await _repository.Find(a => a.UserId == userId);

        //    return _mapper.Map<List<AlertDto>>(alerts);
        //}
        public async Task<List<AlertDto>> GetUserAlerts(int userId)
        {
            var alerts = await _alertRepository.Find(a => a.UserId == userId);

            return _mapper.Map<List<AlertDto>>(alerts);
        }
        public async Task<int> GetUnreadAlertsCount(int userId)
        {
            var unreadAlerts = await _alertRepository.Find(a => a.UserId == userId && !a.IsRead);
            return unreadAlerts.Count();
        }
        public async Task CreateAlert(AlertCreateDto dto, int userId)
        {
            var alert = _mapper.Map<Alert>(dto);

            alert.UserId = userId;
            alert.CreatedAt = DateTime.UtcNow;
            await _alertRepository.Add(alert);

        }

        public async Task MarkAsRead(int alertId, int userId)
        {
            var alert = await _alertRepository.GetById(alertId);
            if (alert == null)
                throw new KeyNotFoundException($"Alert with ID {alertId} not found.");

            if (alert.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to modify this alert.");

            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;

            await _alertRepository.Update(alertId, alert);
        }


    }
}