using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IAnnouncementRepository
{
    Task<List<Announcement>> GetActiveAnnouncementsAsync();
    Task<Announcement?> GetByIdAsync(Guid id);
    Task AddAsync(Announcement entity);
    void Update(Announcement entity);
    void Delete(Announcement entity);
    Task SaveChangesAsync();
}
