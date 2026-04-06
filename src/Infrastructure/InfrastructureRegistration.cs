using Application.Common.Interfaces;
using Application.UseCases.Problems;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Domain.Abstractions;
using Infrastructure.Configurations.FileStorage;
using Infrastructure.ExternalServices;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Common.Repositories;
using Infrastructure.Persistence.Repositories.Problems;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IProblemDiscussionRepository, ProblemDiscussionRepository>();
        services.AddScoped<IContentReportRepository, ContentReportRepository>();
        services.AddScoped<IDiscussionCommentRepository, DiscussionCommentRepository>();
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

    public static IServiceCollection AddExternalServices(this IServiceCollection services , IConfiguration config)
    {
        // Email
        services.Configure<Infrastructure.Configurations.Auth.EmailSettings>(config.GetSection("EmailSettings"));
        services.AddScoped<Application.Abstractions.Outbound.Services.IEmailService , Infrastructure.ExternalServices.Mailing.EmailService>();

        // Cloudinary
        services.Configure<CloudinarySettings>(config.GetSection("FileStorage:CloudinarySettings"));
        services.AddScoped<Application.Abstractions.Outbound.Services.ICloudinaryService , Infrastructure.ExternalServices.FileStorage.CloudinaryService>();

        // Cloudflare R2
        services.Configure<R2Settings>(config.GetSection("FileStorage:R2Settings"));
        services.AddScoped<Application.Abstractions.Outbound.Services.IR2Service , Infrastructure.ExternalServices.FileStorage.R2Service>();

        //  HttpClient
        services.AddHttpClient();
        return services;
    }
}
