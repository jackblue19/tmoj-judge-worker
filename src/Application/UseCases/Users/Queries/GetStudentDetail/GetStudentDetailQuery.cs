using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.GetStudentDetail;

public record GetStudentDetailQuery(Guid UserId, Guid? SemesterId, Guid? SubjectId) : IRequest<StudentProfileWithClassesDto?>;

public class GetStudentDetailQueryHandler : IRequestHandler<GetStudentDetailQuery, StudentProfileWithClassesDto?>
{
    private readonly IUserManagementRepository _repo;

    public GetStudentDetailQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public Task<StudentProfileWithClassesDto?> Handle(GetStudentDetailQuery req, CancellationToken ct) =>
        _repo.GetStudentDetailAsync(req.UserId, req.SemesterId, req.SubjectId, ct);
}
