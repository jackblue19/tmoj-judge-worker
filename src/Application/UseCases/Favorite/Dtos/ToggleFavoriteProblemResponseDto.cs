using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.UseCases.Favorite.Dtos;

namespace Application.UseCases.Favorite.Dtos
{
    public class ToggleFavoriteProblemResponseDto
    {
        public Guid ProblemId { get; set; }
        public bool IsFavorited { get; set; }
        public string Action { get; set; } = default!;

        // ✅ NEW
        public bool IsSuccess { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public CollectionInfoDto? Collection { get; set; }
    }
}
