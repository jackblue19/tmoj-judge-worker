using MediatR;

namespace Application.UseCases.Contests.Commands;

public class PublishRankingCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }
}
