using BeverageDistributor.Application.DTOs.Distributor;
using BeverageDistributor.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace BeverageDistributor.API.Controllers
{
    /// <summary>
    /// Controller responsável por gerenciar as operações relacionadas a distribuidores de bebidas.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class DistributorsController : ControllerBase
    {
        private readonly IDistributorService _distributorService;

        public DistributorsController(IDistributorService distributorService)
        {
            _distributorService = distributorService;
        }

        /// <summary>
        /// Obtém a lista de todos os distribuidores cadastrados.
        /// </summary>
        /// <returns>Retorna a lista de distribuidores.</returns>
        /// <response code="200">Retorna a lista de distribuidores com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DistributorResponseDto>))]
        public async Task<ActionResult<IEnumerable<DistributorResponseDto>>> GetAll()
        {
            var distributors = await _distributorService.GetAllAsync();
            return Ok(distributors);
        }

        /// <summary>
        /// Obtém um distribuidor específico pelo seu identificador único.
        /// </summary>
        /// <param name="id">O identificador único do distribuidor (GUID).</param>
        /// <returns>Retorna os detalhes do distribuidor.</returns>
        /// <response code="200">Retorna o distribuidor solicitado.</response>
        /// <response code="404">Distribuidor não encontrado.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DistributorResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DistributorResponseDto>> GetById(
            [Required(ErrorMessage = "O ID do distribuidor é obrigatório")]
            Guid id)
        {
            var distributor = await _distributorService.GetByIdAsync(id);
            if (distributor == null)
            {
                return NotFound();
            }
            return Ok(distributor);
        }

        /// <summary>
        /// Cria um novo distribuidor no sistema.
        /// </summary>
        /// <param name="createDto">Dados para criação do distribuidor.</param>
        /// <returns>Retorna os dados do distribuidor criado.</returns>
        /// <response code="201">Distribuidor criado com sucesso.</response>
        /// <response code="400">Dados inválidos fornecidos.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DistributorResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DistributorResponseDto>> Create(
            [FromBody, Required(ErrorMessage = "Os dados do distribuidor são obrigatórios")]
            CreateDistributorDto createDto)
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
