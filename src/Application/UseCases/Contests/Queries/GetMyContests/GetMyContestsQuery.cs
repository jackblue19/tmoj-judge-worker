using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetMyContestsQuery : IRequest<List<MyContestDto>>
{
    public string? Status { get; set; }
}