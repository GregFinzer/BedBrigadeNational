using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewslettersController : LocationScopedRepositoryControllerBase<Newsletter, int, INewsletterDataService>
{
    public NewslettersController(INewsletterDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.NewsletterId)
    {
    }

    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpGet]
    [SwaggerOperation("GetNewsletters")]
    public async Task<ActionResult<List<Newsletter>>> GetAllAsync() => await GetScopedAllCoreAsync();

    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetNewsletter")]
    public async Task<ActionResult<Newsletter>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpPost]
    [SwaggerOperation("CreateNewsletter")]
    public async Task<ActionResult<Newsletter>> CreateAsync([FromBody] Newsletter newsletter) =>
        await CreateScopedCoreAsync(newsletter);

    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateNewsletter")]
    public async Task<ActionResult<Newsletter>> UpdateAsync(int id, [FromBody] Newsletter newsletter) =>
        await UpdateScopedCoreAsync(id, newsletter);

    [Authorize(Roles = RoleNames.CanManageNewsletters)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteNewsletter")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
