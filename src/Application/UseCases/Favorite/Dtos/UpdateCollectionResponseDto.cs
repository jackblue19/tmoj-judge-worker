using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos
{
    public class UpdateCollectionResponseDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsVisibility { get; set; }

        public bool IsSuccess { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
