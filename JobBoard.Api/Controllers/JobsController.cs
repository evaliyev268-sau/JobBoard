using JobBoard.Application.DTOs;
using JobBoard.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _service;

        public JobsController(IJobService service)
        {
            _service = service;
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<JobDto>> GetById(int id, CancellationToken ct)
        {
            var item = await _service.GetByIdAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }
        [HttpGet]
        public async Task<ActionResult<List<JobDto>>> GetAll(CancellationToken ct)
        {
            return Ok(await _service.GetAllAsync(ct));
        }
        [HttpPost]
        public async Task<ActionResult<JobDto>> Create([FromBody] CreateJobRequest req, CancellationToken ct)
        {
            var created = await _service.CreateAsync(req, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPost("{id:int}/apply")]
        public async Task<ActionResult> Apply(int id, [FromBody] ApplyRequest req, CancellationToken ct)
        {
            var appId = await _service.ApplyAsync(id, req, ct);
            return Created($"/api/jobs/{id}/apply/{appId}", new { Id = appId });
        }

        [HttpGet("{id:int}/applications")]
        public async Task<ActionResult<List<JobApplicationDto>>> GetApplications(int id, CancellationToken ct)
        {
            var apps=await _service.GetApplicationAsync(id, ct);
            return Ok(apps);
        }





    }
}
