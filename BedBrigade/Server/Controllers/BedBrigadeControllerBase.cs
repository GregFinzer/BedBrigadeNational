using BedBrigade.Server.Data;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Mvc;
using BedBrigade.Shared;
using System.Data.Common;

namespace BedBrigade.Server.Controllers
{
    /// <summary>
    /// This controller handles basic CRUD API requests and should be applied to each BedBrigage Server Controller
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BedBrigadeControllerBase<T> : ControllerBase where T : class
    {
        private readonly DataContext _context;
        private readonly ILogger<T> _logger;
        private ServiceResponse<T> response = new() { Success = false, Message = string.Empty, Data = null };

        public BedBrigadeControllerBase(DataContext context, ILogger<T> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllAsync()
        {
            var result = _context.Set<T>();
            response.Data = result;


        }

        [HttpGet()]
        public async Task<ActionResult> GetAsync(int Id)
        {
            var result = _context.Set<T>().Where( x => x.Id == Id);
            response.Data = result;


        }


        [HttpPost]
        public async Task<ActionResult> CreateAsync(T objToCreate)
        {
            if (objToCreate == null)
            {
                response.Message = "No record data passed in";
                return BadRequest(response);
            }
            else
            {
                try
                {
                    await _context.AddAsync(objToCreate);
                    await _context.SaveChangesAsync();
                    response.Success = true;
                    response.Data = objToCreate;
                    return Ok(response);
                }
                catch (DbException ex)
                {
                    response.Message = $"Database exception {ex.Message}";
                    return StatusCode(StatusCodes.Status500InternalServerError, response);
                }
            }

        }
    }
}
