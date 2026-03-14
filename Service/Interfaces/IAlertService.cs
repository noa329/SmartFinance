using Common.Dto;

public interface IAlertService
{
    Task<List<AlertDto>> GetUserAlerts(int userId);

    Task CreateAlert(AlertCreateDto dto, int userId);

    Task MarkAsRead(int alertId, int userId);
}