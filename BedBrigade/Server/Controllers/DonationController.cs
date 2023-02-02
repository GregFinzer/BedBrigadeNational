using BedBrigade.Admin.Services;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BedBrigade.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DonationController : ControllerBase
{
    private readonly Services.IDonationService _donationService;
    private readonly ILogger<DonationController> _logger;

    public DonationController(Services.IDonationService locationService, ILogger<DonationController> logger)
    {
        _donationService = locationService;
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
        var response = await _donationService.GetAsync(id);
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
        var response = await _donationService.GetAllAsync();
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
        var response = await _donationService.DeleteAsync(UserName);
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
    public async Task<IActionResult> Update([FromBody] Donation location)
    {
        var response = await _donationService.UpdateAsync(location);
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
    public async Task<IActionResult> Create([FromBody] Donation location)
    {
        var response = await _donationService.CreateAsync(location);
        if (response.Success)
        {
            return Ok(response);
        }
        return BadRequest(response);

    }


}
