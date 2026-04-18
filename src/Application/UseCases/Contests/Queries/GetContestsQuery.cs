using MediatR;
using Application.UseCases.Contests.Dtos;
using Application.Common.Models;

namespace Application.UseCases.Contests.Queries;

public class GetContestsQuery : IRequest<PagedResult<ContestDto>>
{
    public string? Status { get; set; }

    public string? VisibilityCode { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}