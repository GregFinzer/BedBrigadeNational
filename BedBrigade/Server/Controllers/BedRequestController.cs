using BedBrigade.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BedBrigade.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BedRequestController : ControllerBase
    {
        private readonly Services.IBedRequestService _bedRequestService;
        private readonly ILogger<BedRequestController> _logger;

        public BedRequestController(Services.IBedRequestService locationService, ILogger<BedRequestController> logger)
        {
            _bedRequestService = locationService;
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
        public async Task<ActionResult<ServiceResponse<BedRequest>>> Get(int id)
        {
            var response = await _bedRequestService.GetAsync(id);
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
            var response = await _bedRequestService.GetAllAsync();
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
            var response = await _bedRequestService.DeleteAsync(UserName);
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
        public async Task<IActionResult> Update([FromBody] BedRequest location)
        {
            var response = await _bedRequestService.UpdateAsync(location);
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
        public async Task<IActionResult> Create([FromBody] BedRequest location)
        {
            var response = await _bedRequestService.CreateAsync(location);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);

        }


    }
}
