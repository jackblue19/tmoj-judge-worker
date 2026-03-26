using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.Editorials.Specs;

namespace Application.UseCases.Editorials;

public class CreateEditorialCommandHandler
    : IRequestHandler<CreateEditorialCommand, Guid>
{
    private readonly IWriteRepository<Editorial, Guid> _writeRepo;
    private readonly IReadRepository<Editorial, Guid> _readRepo;
    private readonly IReadRepository<Problem, Guid> _problemRepo;
    private readonly IReadRepository<StorageFile, Guid> _storageRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateEditorialCommandHandler(
        IWriteRepository<Editorial, Guid> writeRepo,
        IReadRepository<Editorial, Guid> readRepo,
        IReadRepository<Problem, Guid> problemRepo,
        IReadRepository<StorageFile, Guid> storageRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
        _problemRepo = problemRepo;
        _storageRepo = storageRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateEditorialCommand request, CancellationToken ct)
    {
        if (request.ProblemId == Guid.Empty)
            throw new Exception("ProblemId is required");

        if (request.StorageId == Guid.Empty)
            throw new Exception("StorageId is required");

        // 🔥 CLEAN: lấy user từ service
        var userId = _currentUser.UserId;
        if (userId == null)
            throw new UnauthorizedAccessException();

        var problem = await _problemRepo.GetByIdAsync(request.ProblemId, ct);
        if (problem == null)
            throw new Exception("Problem not found");

        var storage = await _storageRepo.GetByIdAsync(request.StorageId, ct);
        if (storage == null)
            throw new Exception("Storage not found");

        var exists = await _readRepo.AnyAsync(
            new EditorialByProblemSpec(request.ProblemId), ct);

        if (exists)
            throw new Exception("Editorial already exists");

        var editorial = new Editorial
        {
            EditorialId = Guid.NewGuid(),
            ProblemId = request.ProblemId,
            StorageId = request.StorageId,
            AuthorId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _writeRepo.AddAsync(editorial, ct);
        await _uow.SaveChangesAsync(ct);

        return editorial.EditorialId;
    }
}