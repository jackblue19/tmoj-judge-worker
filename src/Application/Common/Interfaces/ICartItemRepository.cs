using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface ICartItemRepository
{
    Task<CartItem?> GetByUserAndItemAsync(Guid userId, Guid itemId);
    Task<List<CartItem>> GetByUserIdAsync(Guid userId);
    Task AddAsync(CartItem entity);
    void Update(CartItem entity);
    void Remove(CartItem entity);
    void RemoveRange(IEnumerable<CartItem> entities);
}
