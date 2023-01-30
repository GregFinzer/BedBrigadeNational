using BedBrigade.Admin.Services;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BedBrigade.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly Services.IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(Services.IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("{UserName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ServiceResponse<User>>> Get(string UserName)
        {
            if (string.IsNullOrEmpty(UserName))
            {
                return BadRequest(new ServiceResponse<User>("UserName is empty or null."));
            }
            var response = await _userService.GetAsync(UserName);
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
            var response = await _userService.GetAllAsync();
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
            var response = await _userService.DeleteAsync(UserName);
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
        public async Task<IActionResult> Update(User user)
        {
            var response = await _userService.UpdateAsync(user);
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
        public async Task<IActionResult> Create([FromBody] User user)
        {
            var response = await _userService.CreateAsync(user);
            if (response.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);

        }


    }
}
