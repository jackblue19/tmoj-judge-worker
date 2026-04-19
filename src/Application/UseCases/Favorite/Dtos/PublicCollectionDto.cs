using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos
{
    public class PublicCollectionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public string Type { get; set; } = default!;
        public bool IsVisibility { get; set; }

        public DateTime CreatedAt { get; set; }

        // ✅ NEW
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = default!;

        public int TotalItems { get; set; }

        // ✅ NEW
        public List<PreviewItemDto> PreviewItems { get; set; } = new();
    }

}
