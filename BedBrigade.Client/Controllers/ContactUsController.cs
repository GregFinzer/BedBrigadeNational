using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactUsController : LocationScopedRepositoryControllerBase<ContactUs, int, IContactUsDataService>
{
    public ContactUsController(IContactUsDataService dataService, ILocationDataService locationDataService)
        : base(dataService, locationDataService, x => x.ContactUsId)
    {
    }

    [Authorize(Roles = RoleNames.CanViewContacts)]
    [HttpGet]
    [SwaggerOperation("GetContactUs")]
    public async Task<ActionResult<List<ContactUs>>> GetAllAsync() => await GetScopedAllCoreAsync();

    [Authorize(Roles = RoleNames.CanViewContacts)]
    [HttpGet("{id:int}")]
    [SwaggerOperation("GetContactUsById")]
    public async Task<ActionResult<ContactUs>> GetByIdAsync(int id) => await GetScopedByIdCoreAsync(id);

    [Authorize(Roles = RoleNames.CanManageContacts)]
    [HttpPost]
    [SwaggerOperation("CreateContactUs")]
    public async Task<ActionResult<ContactUs>> CreateAsync([FromBody] ContactUs contactUs) =>
        await CreateScopedCoreAsync(contactUs);

    [Authorize(Roles = RoleNames.CanManageContacts)]
    [HttpPut("{id:int}")]
    [SwaggerOperation("UpdateContactUs")]
    public async Task<ActionResult<ContactUs>> UpdateAsync(int id, [FromBody] ContactUs contactUs) =>
        await UpdateScopedCoreAsync(id, contactUs);

    [Authorize(Roles = RoleNames.CanManageContacts)]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("DeleteContactUs")]
    public async Task<IActionResult> DeleteAsync(int id) => await DeleteScopedCoreAsync(id);
}
