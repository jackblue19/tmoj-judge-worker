using MediatR;
using Application.UseCases.ClassSlots.Dtos;

namespace Application.UseCases.ClassSlots.Queries;

public class GetClassSlotRankingsQuery : IRequest<ClassSlotRankingDto>
{
    public Guid ClassSlotId { get; set; }
}
