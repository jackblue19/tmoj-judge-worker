using Application.UseCases.ProblemDiscussions.Dtos;
using MediatR;

namespace Application.UseCases.ProblemDiscussions.Queries
{
    public class GetDiscussionByIdQuery : IRequest<DiscussionResponseDto>
    {
        public Guid Id { get; set; }
    }
}