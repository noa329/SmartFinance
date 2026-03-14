using AutoMapper;
using Common.Dto;
using Microsoft.EntityFrameworkCore;
using Repository.Entities;
using Repository.Interfaces;
using Repository.Repositories;

public class AlertService : IAlertService
{
    private readonly IRepository<Alert> _alertRepository;
    private readonly IMapper _mapper;
    private readonly IContext _context;

    public AlertService(
        IRepository<Alert> repository,
        IMapper mapper,
        IContext context)
    {
        _alertRepository = repository;
        _mapper = mapper;
        _context = context;
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
        return await _context.Alerts
            .Where(a => a.UserId == userId && !a.IsRead)
            .CountAsync();
    }
    public async Task CreateAlert(AlertCreateDto dto, int userId)
    {
        var alert = _mapper.Map<Alert>(dto);

        alert.UserId = userId;

        await _alertRepository.Add(alert);
        await _context.Save();

    }

    public async Task MarkAsRead(int alertId, int userId)
    {
        var alert = await _alertRepository.GetById(alertId);

        if (alert == null || alert.UserId != userId)
            throw new Exception("Unauthorized");

        alert.IsRead = true;
        alert.ReadAt = DateTime.UtcNow;

        await _alertRepository.Update(alertId, alert);
        await _context.Save();

    }


}