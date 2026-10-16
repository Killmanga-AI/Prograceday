using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
namespace RaceDay.Api.Controllers;
[ApiController]
public abstract class ApiControllerBase : ControllerBase {
 protected int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
}
