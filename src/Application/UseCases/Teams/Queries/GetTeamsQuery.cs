using Application.UseCases.Teams.Dtos;
using MediatR;

namespace Application.UseCases.Teams.Queries;

public class GetTeamsQuery : IRequest<List<TeamDto>>
{
}