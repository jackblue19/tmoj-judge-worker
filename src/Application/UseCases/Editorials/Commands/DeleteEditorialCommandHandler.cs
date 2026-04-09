using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Editorials.Commands;

public class DeleteEditorialCommandHandler
    : IRequestHandler<DeleteEditorialCommand, Unit>
{
    private readonly IWriteRepository<Editorial, Guid> _writeRepo;
    private readonly IReadRepository<Editorial, Guid> _readRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public DeleteEditorialCommandHandler(
        IWriteRepository<Editorial, Guid> writeRepo,
        IReadRepository<Editorial, Guid> readRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteEditorialCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null)
            throw new UnauthorizedAccessException();

        var editorial = await _readRepo.GetByIdAsync(request.EditorialId, ct);
        if (editorial == null)
            throw new Exception("Editorial not found");

        // 🔥 Permission
        if (editorial.AuthorId != userId && !_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("You cannot delete this editorial");

        _writeRepo.Remove(editorial);
        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}