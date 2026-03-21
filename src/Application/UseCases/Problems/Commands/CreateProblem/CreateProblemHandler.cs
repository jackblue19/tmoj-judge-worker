using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed class CreateProblemHandler
    : IRequestHandler<CreateProblemCommand , Guid>
{
    private readonly IWriteRepository<Problem , Guid> _repo;
    private readonly IUnitOfWork _uow;

    public CreateProblemHandler(
        IWriteRepository<Problem , Guid> repo ,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Guid> Handle(
        CreateProblemCommand request ,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var entity = new Problem
        {
            Id = Guid.NewGuid() ,
            Title = request.Title ,
            Slug = request.Slug ,
            Difficulty = request.Difficulty ,
            TypeCode = request.TypeCode ,
            VisibilityCode = request.VisibilityCode ,
            ScoringCode = request.ScoringCode ,
            StatusCode = request.StatusCode ,
            DescriptionMd = request.DescriptionMd ,
            TimeLimitMs = request.TimeLimitMs ,
            MemoryLimitKb = request.MemoryLimitKb ,
            IsActive = true ,
            CreatedAt = now ,
            PublishedAt = request.StatusCode == "published"
                ? now
                : null
        };

        await _repo.AddAsync(entity , ct);

        await _uow.SaveChangesAsync(ct);

        return entity.Id;
    }
}