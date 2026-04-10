using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Application.Common.Interfaces
{
    public interface IEditorialRepository
    {
        Task<Editorial?> GetByIdAsync(Guid id);

        Task<List<Editorial>> GetByProblemIdAsync(Guid problemId);

        Task<Guid> CreateAsync(Editorial editorial);

        void Update(Editorial editorial);

        void Delete(Editorial editorial);

        Task SaveChangesAsync();
    }
}