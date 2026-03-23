using Ardalis.Specification.EntityFrameworkCore;
using Ardalis.Specification;
using Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence.Common;

namespace Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddPostgresConnection(this IServiceCollection services ,
                                                    IConfiguration config)
    {
        services.AddDbContext<TmojDbContext>(options =>
        {
            options.UseNpgsql(config.GetConnectionString("TmojPostgres") ,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(TmojDbContext).Assembly.FullName);
                });
        });

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        //  Generic repos
        services.AddSingleton<ISpecificationEvaluator>(SpecificationEvaluator.Default);
        services.AddScoped(typeof(IReadRepository<,>) , typeof(EfReadRepository<,>));
        services.AddScoped(typeof(IWriteRepository<,>) , typeof(EfWriteRepository<,>));
        services.AddScoped<IUnitOfWork , EfUnitOfWork>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services ,
        IConfiguration config)
    {
        //  repos

        return services;
    }

    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<Infrastructure.Configurations.Auth.EmailSettings>(config.GetSection("EmailSettings"));
        services.AddScoped<Application.Abstractions.Outbound.Services.IEmailService, Infrastructure.ExternalServices.Mailing.EmailService>();

        return services;
    }
}
