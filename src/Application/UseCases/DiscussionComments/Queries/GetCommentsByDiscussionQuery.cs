using MediatR;
using Application.UseCases.DiscussionComments.Dtos;

namespace Application.UseCases.DiscussionComments.Queries;

public record GetCommentsByDiscussionQuery(Guid DiscussionId)
    : IRequest<List<CommentResponseDto>>;