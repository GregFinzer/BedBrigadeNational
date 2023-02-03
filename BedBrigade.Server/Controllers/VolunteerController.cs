using BedBrigade.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BedBrigade.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VolunteerController : ControllerBase
{
    private readonly Services.IVolunteerService _volunteerService;
    private readonly ILogger<VolunteerController> _logger;

    public VolunteerController(Services.IVolunteerService locationService, ILogger<VolunteerController> logger)
    {
        _volunteerService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceResponse<User>>> Get(int id)
    {
        var response = await _volunteerService.GetAsync(id);
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceResponse<List<User>>>> GetAll()
    {
        var response = await _volunteerService.GetAllAsync();
        if (response.Success)
        {
            return Ok(response);
        }
        return NotFound(response);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ServiceResponse<bool>>> Delete(string UserName)
    {
        var response = await _volunteerService.DeleteAsync(UserName);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update([FromBody] Volunteer location)
    {
        var response = await _volunteerService.UpdateAsync(location);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] Volunteer location)
    {
        var response = await _volunteerService.CreateAsync(location);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);

    }


}
