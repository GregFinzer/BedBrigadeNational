using BedBrigade.Server.Data;
using BedBrigade.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<ContentController> _logger;
        public ContentController(DataContext context, ILogger<ContentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("type/{contentType}/name/{name}")]
        public async Task<ActionResult> Get(string contentType, string name)
        {
            _logger.LogTrace("Attempting to retrieve content for content type {0}, name {1}", contentType, name);

            var content = await _context.Content.FirstOrDefaultAsync(c => c.ContentType == contentType && c.Name == name);
            if (content is null || string.IsNullOrEmpty(content.ContentType))
                return NotFound();
            return Ok(content.ContentHtml);
        }
    }
}
