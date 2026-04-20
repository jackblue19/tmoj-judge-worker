using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Helpers;

namespace Application.UseCases.Contests.Commands;

public class CreateContestCommandHandler
    : IRequestHandler<CreateContestCommand, Guid>
{
    private readonly IWriteRepository<Contest, Guid> _writeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public CreateContestCommandHandler(
        IWriteRepository<Contest, Guid> writeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _writeRepo = writeRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateContestCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        if (!userId.HasValue)
            throw new UnauthorizedAccessException();

        // ===============================
        // VALIDATION
        // ===============================
        if (request.StartAt >= request.EndAt)
            throw new Exception("Start time must be before end time");

        var visibility = (request.VisibilityCode ?? "private").Trim().ToLower();
        var valid = new[] { "public", "private", "hidden" };
        if (!valid.Contains(visibility))
            throw new ArgumentException("INVALID_VISIBILITY");

        // ===============================
        // CREATE CONTEST
        // ===============================
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var contest = new Contest
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            DescriptionMd = request.Description,

            StartAt = DateTime.SpecifyKind(request.StartAt, DateTimeKind.Utc),
            EndAt = DateTime.SpecifyKind(request.EndAt, DateTimeKind.Utc),

            VisibilityCode = visibility,
            ContestType = request.ContestType,
            AllowTeams = request.AllowTeams,
            IsActive = true,

            // Mọi contest đều gen invite code — public contest cũng join được bằng code.
            InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),

            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        await _writeRepo.AddAsync(contest, ct);
        await _uow.SaveChangesAsync(ct);

        return contest.Id;
    }
}