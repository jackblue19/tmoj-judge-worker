using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Pagination
{
    public class CursorPaginationDto<T>
    {
        public List<T> Items { get; set; } = new();

        // Cursor cho page tiếp theo
        public DateTime? NextCursorCreatedAt { get; set; }
        public Guid? NextCursorId { get; set; }

        // Có còn data không
        public bool HasMore { get; set; }
    }
}
