using BedBrigade.Server.Data;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

namespace BedBrigade.Server.Controllers
{
    [Route("api/[controller")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(DataContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("{UserName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(string UserName)
        {
            var result = await _context.Users.FindAsync(UserName);
            if (result != null)
            {
                return Ok(new ServiceResponse<User>("Found Record", true, result));
            }

            return NotFound(new ServiceResponse<User>("Not Found"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "Location").Value ?? "0", out int locationId);
            var result = _context.Users.Where(u => u.Location.LocationId == locationId).ToList();
            if (result != null)
            {
                return Ok(new ServiceResponse<List<User>>($"Found {result.Count} records.", true, result));
            }
            return NotFound(new ServiceResponse<List<User>>("None found."));
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(string UserName)
        {
            var user = await _context.Users.FindAsync(UserName);
            if (user == null)
            {
                return NotFound(new ServiceResponse<User>($"User record with key {UserName} not found"));
            }
            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok(new ServiceResponse<User>($"Removed record with key {UserName}.", true));
            }
            catch (DbException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ServiceResponse<User>($"DB error on delete of user record with key {UserName} - {ex.Message} ({ex.ErrorCode})"));
            }
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(User user)
        {
            var result = _context.Users.Update(user);
            if (result != null)
            {
                return Ok(new ServiceResponse<User>($"Updated record with user key {user.UserName}", true));
            }
            return BadRequest(new ServiceResponse<User>($"User record with key {user.UserName} was not updated."));

        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                return Ok(new ServiceResponse<User>($"Added user record with key {user.UserName}.", true));
            }
            catch (DbException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ServiceResponse<User>($"DB error on delete of user record with key {user.UserName} - {ex.Message} ({ex.ErrorCode})"));
            }
        }


    }
}
