using Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IFptItemRepository
{
    Task<FptItem?> GetByIdAsync(Guid id);
    Task<List<FptItem>> GetAllActiveAsync();
    Task AddAsync(FptItem entity);
    Task UpdateAsync(FptItem entity);
    Task DeleteAsync(FptItem entity);
}
