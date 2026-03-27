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
        Task<List<Editorial>> GetByProblemIdAsync(Guid problemId);
        Task<Editorial?> GetByIdAsync(Guid id);
        Task AddAsync(Editorial editorial);
        void Update(Editorial editorial);
        void Remove(Editorial editorial);
    }
}
