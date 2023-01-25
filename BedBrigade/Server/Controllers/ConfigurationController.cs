using BedBrigade.Server.Data;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Data.Common;

namespace BedBrigade.Server.Controllers
{
    [Route("api/[controller")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(DataContext context, ILogger<ConfigurationController> logger)
        {
            _context = context;
            _logger = logger;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("{ConfigurationKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(string ConfigurationKey)
        {
            var result = await _context.Configurations.FindAsync(ConfigurationKey);
            if (result != null)
            {
                return Ok(new ServiceResponse<Configuration>("Found Record", true, result));
            }

            return NotFound(new ServiceResponse<Configuration>("Not Found"));
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
            var result = _context.Configurations.ToList();
            if (result != null)
            {
                return Ok(new ServiceResponse<List<Configuration>>($"Found {result.Count} records.", true, result));
            }
            return NotFound(new ServiceResponse<List<Configuration>>("None found."));
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(string ConfigurationKey)
        {
            var configuration = await _context.Configurations.FindAsync(ConfigurationKey);
            if (configuration == null)
            {
                return NotFound(new ServiceResponse<Configuration>($"Configuration record with key {ConfigurationKey} not found"));
            }
            try
            {
                _context.Configurations.Remove(configuration);
                await _context.SaveChangesAsync();
                return Ok(new ServiceResponse<Configuration>($"Removed record with key {ConfigurationKey}.", true));
            }
            catch (DbException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ServiceResponse<Configuration>($"DB error on delete of configuration record with key {ConfigurationKey} - {ex.Message} ({ex.ErrorCode})"));
            }
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(Configuration configuration)
        {
            var result = _context.Configurations.Update(configuration);
            if (result != null)
            {
                return Ok(new ServiceResponse<Configuration>($"Updated record with configuration key {configuration.ConfigurationKey}", true));
            }
            return BadRequest(new ServiceResponse<Configuration>($"Configuration record with key {configuration.ConfigurationKey} was not updated."));

        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] Configuration configuration)
        {
            try
            {
                await _context.Configurations.AddAsync(configuration);
                await _context.SaveChangesAsync();
                return Ok(new ServiceResponse<Configuration>($"Added configuration record with key {configuration.ConfigurationKey}.", true));
            }
            catch (DbException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ServiceResponse<Configuration>($"DB error on delete of configuration record with key {configuration.ConfigurationKey} - {ex.Message} ({ex.ErrorCode})"));
            }
        }


    }
}

