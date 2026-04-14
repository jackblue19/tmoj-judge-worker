using Application.UseCases.Contests.Dtos;
using MediatR;

public class GetMyContestsQuery : IRequest<List<MyContestDto>>
{
    public string? Status { get; set; }
}