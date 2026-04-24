using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos;

public class CollectionItemDto
{
    public Guid Id { get; set; }

    public Guid? ProblemId { get; set; }
    public string? ProblemTitle { get; set; }
    public string? ProblemDifficulty { get; set; }
    public Guid? ContestId { get; set; }
    public string? ContestTitle { get; set; }

    public DateTime CreatedAt { get; set; }

    // ✅ NEW
    public bool IsSolved { get; set; }
    
    // ✅ NEW: Frontend cần để biết hiển thị ổ khóa
    public bool IsPrivate { get; set; }
}