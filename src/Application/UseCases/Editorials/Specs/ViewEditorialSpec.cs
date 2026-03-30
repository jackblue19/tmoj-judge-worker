using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Editorials.Specs
{
    public class ViewEditorialSpec
     : Specification<Editorial, EditorialDto>
    {
        public ViewEditorialSpec(
            Guid problemId,
            Guid? cursorId,
            DateTime? cursorCreatedAt,
            int pageSize)
        {
            Query.Include(e => e.Storage);

            // Filter theo Problem
            Query.Where(e => e.ProblemId == problemId);

            // Cursor pagination
            if (cursorId.HasValue && cursorCreatedAt.HasValue)
            {
                Query.Where(e =>
                    e.CreatedAt < cursorCreatedAt.Value ||
                    (e.CreatedAt == cursorCreatedAt.Value && e.EditorialId < cursorId.Value));
            }

            Query
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.EditorialId)
                .Take(pageSize);

            Query.Select(e => new EditorialDto(
                e.EditorialId,
                e.ProblemId,
                e.AuthorId,
                e.Storage.FilePath,
                e.Storage.FileType,
                e.CreatedAt
            ));
        }
    }
}
