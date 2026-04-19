using MediatR;
using Application.UseCases.StudyProgress.Dtos;

namespace Application.UseCases.StudyProgress.Queries.GetMyStudyProgress;

public class GetMyStudyProgressQuery : IRequest<MyStudyProgressDto>
{
    public Guid UserId { get; set; }
}