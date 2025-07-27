using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Threading.Tasks;

namespace BeverageDistributor.API.Controllers
{
    /// <summary>
    /// Controlador responsável por fornecer endpoints para verificação de saúde da aplicação.
    /// Este controlador expõe informações sobre o status de saúde dos serviços e dependências da aplicação.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "Monitoramento")]
    [Produces("application/json")]
    public class HealthCheckController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthCheckController> _logger;

        public HealthCheckController(
            HealthCheckService healthCheckService,
            ILogger<HealthCheckController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        /// <summary>
        /// Obtém o status de saúde atual da aplicação e suas dependências.
        /// </summary>
        /// <remarks>
        /// Este endpoint realiza verificações de saúde em todos os serviços configurados
        /// e retorna um relatório detalhado do status de cada um.
        /// 
        /// Exemplo de resposta de sucesso (Status 200):
        /// 
        ///     GET /api/healthcheck
        ///     
        ///     {
        ///       "status": "Healthy",
        ///       "results": [
        ///         {
        ///           "name": "sqlserver",
        ///           "status": "Healthy",
        ///           "description": "SQL Server está respondendo normalmente",
        ///           "duration": "00:00:00.1234567"
        ///         }
        ///       ],
        ///       "totalDuration": "00:00:00.1234567"
        ///     }
        ///     
        /// Exemplo de resposta de falha (Status 503):
        /// 
        ///     GET /api/healthcheck
        ///     
        ///     {
        ///       "status": "Unhealthy",
        ///       "results": [
        ///         {
        ///           "name": "sqlserver",
        ///           "status": "Unhealthy",
        ///           "description": "Falha ao conectar ao SQL Server",
        ///           "exception": "Erro de conexão com o banco de dados",
        ///           "duration": "00:00:00.1234567"
        ///         }
        ///       ],
        ///       "totalDuration": "00:00:00.1234567"
        ///     }
        /// </remarks>
        /// <returns>Retorna um relatório detalhado do status de saúde da aplicação.</returns>
        /// <response code="200">A aplicação está saudável e todas as dependências estão funcionando corretamente.</response>
        /// <response code="503">A aplicação está em execução, mas uma ou mais dependências estão com problemas.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable, Type = typeof(object))]
        // [ApiExplorerSettings(Description = "Endpoint para verificação do status de saúde da aplicação e suas dependências.")]
        public async Task<IActionResult> GetHealth()
        {
            _logger.LogInformation("Verificando status de saúde da aplicação");
            
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var status = healthReport.Status == HealthStatus.Healthy 
                ? HttpStatusCode.OK 
                : HttpStatusCode.ServiceUnavailable;

            var response = new
            {
                status = healthReport.Status.ToString(),
                results = healthReport.Entries.Select(e => new 
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    exception = e.Value.Exception?.Message,
                    duration = e.Value.Duration
                }),
                totalDuration = healthReport.TotalDuration
            };

            _logger.LogInformation("Status de saúde: {Status}", healthReport.Status);
            
            return status == HttpStatusCode.OK 
                ? Ok(response) 
                : StatusCode((int)status, response);
        }
    }
}
