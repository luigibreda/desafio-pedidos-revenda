using Microsoft.Extensions.DependencyInjection;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Application.Services;
using FluentValidation;
using System.Reflection;

namespace BeverageDistributor.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddScoped<IDistributorService, DistributorService>();
            return services;
        }
    }
}
