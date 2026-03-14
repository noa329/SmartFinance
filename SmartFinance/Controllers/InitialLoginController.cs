using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.Entities;
using Service.Interfaces;

[ApiController]
[Route("api/initial")]
[Authorize]
public class InitialLoginController : ControllerBase
{
    private readonly IInitialLoginService _service;

    public InitialLoginController(IInitialLoginService service)
    {
        _service = service;
    }

    private int UserId =>
        int.Parse(User.FindFirst("id")!.Value);

    [HttpPost("income")]
    public async Task SetIncome(decimal income)
    {
        await _service.SetInitialIncome(UserId, income);
    }

    [HttpPost("categories/init")]
    public async Task InitCategories()
    {
        await _service.InitializeUserCategories(UserId);
    }
}