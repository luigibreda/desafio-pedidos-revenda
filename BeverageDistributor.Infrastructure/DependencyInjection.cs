using BeverageDistributor.Domain.Interfaces;
using BeverageDistributor.Infrastructure.Persistence;
using BeverageDistributor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeverageDistributor.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, 
                    npgsqlOptions => 
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    }));

            // Register repositories
            services.AddScoped<IDistributorRepository, DistributorRepository>();

            return services;
        }
    }
}
