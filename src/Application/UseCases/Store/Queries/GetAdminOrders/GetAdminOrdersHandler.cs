using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Store.Queries.GetAdminOrders;

public class GetAdminOrdersHandler : IRequestHandler<GetAdminOrdersQuery, PagedResult<AdminOrderDto>>
{
    private readonly IUserInventoryRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetAdminOrdersHandler(IUserInventoryRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AdminOrderDto>> Handle(GetAdminOrdersQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("admin"))
            throw new System.UnauthorizedAccessException();

        return await _repo.GetAdminOrdersAsync(request);
    }
}
