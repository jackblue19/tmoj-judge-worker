using Application.Common.Interfaces;
using Application.UseCases.Users.Dtos;
using MediatR;

namespace Application.UseCases.Users.Queries.GetTeacherDetail;

public record GetTeacherDetailQuery(Guid UserId, Guid? SemesterId, Guid? SubjectId) : IRequest<TeacherDetailDto?>;

public class GetTeacherDetailQueryHandler : IRequestHandler<GetTeacherDetailQuery, TeacherDetailDto?>
{
    private readonly IUserManagementRepository _repo;

    public GetTeacherDetailQueryHandler(IUserManagementRepository repo) => _repo = repo;

    public Task<TeacherDetailDto?> Handle(GetTeacherDetailQuery req, CancellationToken ct) =>
        _repo.GetTeacherDetailAsync(req.UserId, req.SemesterId, req.SubjectId, ct);
}
