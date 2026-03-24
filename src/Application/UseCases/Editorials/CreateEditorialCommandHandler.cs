using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
namespace Application.UseCases.Editorials;

public class CreateEditorialCommandHandler
    : IRequestHandler<CreateEditorialCommand, Guid>
{
    private readonly IWriteRepository<Editorial, Guid> _writeRepo;
    private readonly IReadRepository<Editorial, Guid> _readRepo;
    private readonly IUnitOfWork _uow;

    public CreateEditorialCommandHandler(
        IWriteRepository<Editorial, Guid> writeRepo,
        IReadRepository<Editorial, Guid> readRepo,
        IUnitOfWork uow)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateEditorialCommand request, CancellationToken ct)
    {
        //  1. Validate (basic)
        if (request.ProblemId == Guid.Empty)
            throw new Exception("ProblemId is required");

        if (request.StorageId == Guid.Empty)
            throw new Exception("StorageId is required");

        //  2. Create entity
        var editorial = new Editorial
        {
            EditorialId = Guid.NewGuid(),
            ProblemId = request.ProblemId,
            StorageId = request.StorageId,
            CreatedAt = DateTime.UtcNow
        };

        //  3. Save
        await _writeRepo.AddAsync(editorial, ct);
        await _uow.SaveChangesAsync(ct);

        return editorial.EditorialId;
    }
}
