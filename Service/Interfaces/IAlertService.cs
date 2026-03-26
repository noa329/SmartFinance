using Common.Dto;

namespace Service.Interfaces
{
    public interface IAlertService
    {
        Task<List<AlertDto>> GetUserAlerts(int userId);

        Task CreateAlert(AlertCreateDto dto, int userId);

        Task MarkAsRead(int alertId, int userId);
    }
}