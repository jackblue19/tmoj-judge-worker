using System;

namespace Application.UseCases.Editorials.Dtos
{
    public class EditorialDto
    {
        public Guid EditorialId { get; set; }
        public Guid ProblemId { get; set; }
        public Guid? AuthorId { get; set; }
        public Guid StorageId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}