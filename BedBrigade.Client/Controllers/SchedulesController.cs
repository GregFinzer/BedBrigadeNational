using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : LocationScopedRepositoryControllerBase<Schedule, int, IScheduleDataService>
{
    public SchedulesController(IScheduleDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.ScheduleId)
    {
    }

    [Authorize(Roles = RoleNames.CanViewSchedule)]
    [HttpGet]
    [SwaggerOperation("GetSchedules")]
    public async Task<ActionResult<List<Schedule>>> GetAllAsync() => await GetScopedAllCoreAsync();

    [Authorize(Roles = RoleNames.CanViewSchedule)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetSchedule")]
    public async Task<ActionResult<Schedule>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPost]
    [SwaggerOperation("CreateSchedule")]
    public async Task<ActionResult<Schedule>> CreateAsync([FromBody] Schedule schedule) =>
        await CreateScopedCoreAsync(schedule);

    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateSchedule")]
    public async Task<ActionResult<Schedule>> UpdateAsync(int id, [FromBody] Schedule schedule) =>
        await UpdateScopedCoreAsync(id, schedule);

    [Authorize(Roles = RoleNames.CanManageSchedule)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteSchedule")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
