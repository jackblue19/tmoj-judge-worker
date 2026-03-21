using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Application.UseCases.Auth;

internal class Sample
{
}

public sealed record CreateOrderCommand() : IRequest<Guid>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand , Guid>
{
    private readonly ICurrentUserService _currentUser;

    public CreateOrderHandler(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateOrderCommand request , CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        if ( userId == null )
            throw new UnauthorizedAccessException();

        // xử lý logic
        return Guid.NewGuid();
    }
}
