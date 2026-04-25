using Application.Common.Interfaces;
using Application.UseCases.Auth.Hasher;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Users.Commands.CreateUser;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Username,
    List<string>? Roles) : IRequest<Guid>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserManagementRepository _repo;
    private readonly IPasswordHasher _hasher;

    public CreateUserCommandHandler(IUserManagementRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task<Guid> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var email = req.Email.ToLowerInvariant();

        if (await _repo.EmailExistsAsync(email, ct))
            throw new InvalidOperationException("Email already exists");

        Guid? roleId = null;
        if (req.Roles != null && req.Roles.Any())
            roleId = await _repo.GetRoleIdByCodeAsync(req.Roles.First().ToLowerInvariant(), ct);

        roleId ??= await _repo.GetRoleIdByCodeAsync("student", ct);

        var user = new User
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = email,
            Password = _hasher.Hash(req.Password),
            Username = req.Username ?? (email.Split('@')[0] + Random.Shared.Next(1000, 9999).ToString()),
            DisplayName = $"{req.LastName} {req.FirstName}",
            LanguagePreference = "vi",
            Status = true,
            EmailVerified = true,
            RoleId = roleId
        };

        _repo.AddUser(user);
        await _repo.SaveAsync(ct);
        return user.UserId;
    }
}
