using Application.Common.Interfaces;
using Application.UseCases.Editorials;
using Application.UseCases.Editorials.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.UseCases.Editorials;

public class ViewEditorialQueryHandler
    : IRequestHandler<ViewEditorialQuery, IReadOnlyList<EditorialDto>>
{
    private readonly IReadRepository<Editorial, Guid> _repo;

    public ViewEditorialQueryHandler(IReadRepository<Editorial, Guid> repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<EditorialDto>> Handle(ViewEditorialQuery request, CancellationToken ct)
    {
        var spec = new ViewEditorialSpec(
            request.ProblemId,
            request.CursorId,
            request.CursorCreatedAt,
            request.PageSize
        );

        return await _repo.ListAsync(spec, ct);
    }
}