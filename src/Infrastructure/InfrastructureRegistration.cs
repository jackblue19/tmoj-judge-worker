using Ardalis.Specification.EntityFrameworkCore;
using Ardalis.Specification;
using Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence.Common;
using Application.Common.Interfaces;
using Application.UseCases.Problems;
using Infrastructure.Persistence.Repositories.Problems;
using Infrastructure.ExternalServices;

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

        services.AddScoped<ICurrentUserService , CurrentUserService>();

        services.AddScoped<IProblemRepository , ProblemRepository>();
        services.AddScoped<ITagRepository , TagRepository>();
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services ,
        IConfiguration config)
    {
        //  repos

        return services;
    }
}
