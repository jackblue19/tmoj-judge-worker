using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IUserInventoryRepository
{
    Task AddAsync(UserInventory entity);
    Task<List<UserInventory>> GetByUserIdAsync(Guid userId);
}
