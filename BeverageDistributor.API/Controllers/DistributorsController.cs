using BeverageDistributor.Application.DTOs.Distributor;
using BeverageDistributor.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeverageDistributor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DistributorsController : ControllerBase
    {
        private readonly IDistributorService _distributorService;

        public DistributorsController(IDistributorService distributorService)
        {
            _distributorService = distributorService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DistributorResponseDto>>> GetAll()
        {
            var distributors = await _distributorService.GetAllAsync();
            return Ok(distributors);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DistributorResponseDto>> GetById(Guid id)
        {
            var distributor = await _distributorService.GetByIdAsync(id);
            if (distributor == null)
            {
                return NotFound();
            }
            return Ok(distributor);
        }

        [HttpPost]
        public async Task<ActionResult<DistributorResponseDto>> Create(CreateDistributorDto createDto)
        {
            try
            {
                var created = await _distributorService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
