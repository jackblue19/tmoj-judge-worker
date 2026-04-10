
using MediatR;
using Application.UseCases.Editorials.Dtos;

namespace Application.UseCases.Editorials.Queries;

public class ViewEditorialQuery : IRequest<List<EditorialDto>>
{
    public Guid ProblemId { get; set; }
    public Guid? CursorId { get; set; }
    public DateTime? CursorCreatedAt { get; set; }
    public int PageSize { get; set; } = 10;

    public ViewEditorialQuery() { }

    public ViewEditorialQuery(Guid problemId, Guid? cursorId, DateTime? cursorCreatedAt, int pageSize)
    {
        ProblemId = problemId;
        CursorId = cursorId;
        CursorCreatedAt = cursorCreatedAt;
        PageSize = pageSize;
    }
}

