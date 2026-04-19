using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos
{
    public class CopyCollectionResult
    {
        public Guid NewCollectionId { get; set; }
        public int TotalItems { get; set; }

        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
