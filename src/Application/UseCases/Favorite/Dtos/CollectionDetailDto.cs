using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Favorite.Dtos;

    public class CollectionDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string Type { get; set; } = default!;
        public bool IsVisibility { get; set; }

        public List<CollectionItemDto> Items { get; set; } = new();
    }

