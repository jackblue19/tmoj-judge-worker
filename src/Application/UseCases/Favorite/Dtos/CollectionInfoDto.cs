using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos
{
    public class CollectionInfoDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
