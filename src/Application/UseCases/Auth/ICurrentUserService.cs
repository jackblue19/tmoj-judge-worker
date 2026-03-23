using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Auth;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }

    Guid? GetUserIdAsGuid();
    bool TryGetUserIdAsGuid(out Guid userId);

    //Guid? Id { get; }
}
