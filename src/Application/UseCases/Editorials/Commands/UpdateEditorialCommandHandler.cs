using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Editorials.Commands
{
    public class UpdateEditorialCommandHandler
      : IRequestHandler<UpdateEditorialCommand, Unit>
    {
        private readonly IWriteRepository<Editorial, Guid> _writeRepo;
        private readonly IReadRepository<Editorial, Guid> _readRepo;
        private readonly IReadRepository<StorageFile, Guid> _storageRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public UpdateEditorialCommandHandler(
            IWriteRepository<Editorial, Guid> writeRepo,
            IReadRepository<Editorial, Guid> readRepo,
            IReadRepository<StorageFile, Guid> storageRepo,
            IUnitOfWork uow,
            ICurrentUserService currentUser)
        {
            _writeRepo = writeRepo;
            _readRepo = readRepo;
            _storageRepo = storageRepo;
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateEditorialCommand request, CancellationToken ct)
        {
            var userId = _currentUser.UserId;
            if (userId == null)
                throw new UnauthorizedAccessException();

            var editorial = await _readRepo.GetByIdAsync(request.EditorialId, ct);
            if (editorial == null)
                throw new Exception("Editorial not found");

            // 🔥 Permission check
            if (editorial.AuthorId != userId && !_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
                throw new UnauthorizedAccessException("You cannot update this editorial");

            var storage = await _storageRepo.GetByIdAsync(request.StorageId, ct);
            if (storage == null)
                throw new Exception("Storage not found");

            // update
            editorial.StorageId = request.StorageId;
            editorial.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            _writeRepo.Update(editorial);
            await _uow.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}
