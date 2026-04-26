using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos
{
    public class PreviewItemDto
    {
        public Guid ItemId { get; set; }

        public Guid? ProblemId { get; set; }
        public string? ProblemTitle { get; set; }

        public Guid? ContestId { get; set; }
        public string? ContestTitle { get; set; }
        
        // ✅ NEW: Frontend cần để biết hiển thị ổ khóa
        public bool IsPrivate { get; set; }
    }
}
