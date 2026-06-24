using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignUpsController : LocationScopedRepositoryControllerBase<SignUp, int, ISignUpDataService>
{
    public SignUpsController(ISignUpDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.SignUpId)
    {
    }

    [Authorize(Roles = RoleNames.CanViewSignUps)]
    [HttpGet]
    [SwaggerOperation("GetSignUps")]
    public async Task<ActionResult<List<SignUp>>> GetAllAsync() => await GetScopedAllCoreAsync();

    [Authorize(Roles = RoleNames.CanViewSignUps)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetSignUp")]
    public async Task<ActionResult<SignUp>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPost]
    [SwaggerOperation("CreateSignUp")]
    public async Task<ActionResult<SignUp>> CreateAsync([FromBody] SignUp signUp) =>
        await CreateScopedCoreAsync(signUp);

    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateSignUp")]
    public async Task<ActionResult<SignUp>> UpdateAsync(int id, [FromBody] SignUp signUp) =>
        await UpdateScopedCoreAsync(id, signUp);

    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteSignUp")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
