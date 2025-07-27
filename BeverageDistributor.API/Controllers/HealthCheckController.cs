using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Threading.Tasks;

namespace BeverageDistributor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
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
