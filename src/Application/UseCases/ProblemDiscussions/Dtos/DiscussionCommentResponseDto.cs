using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.ProblemDiscussions.Dtos
{
    public class DiscussionCommentResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string? UserEquippedFrameUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }

        // Recursive replies
        public List<DiscussionCommentResponseDto> Children { get; set; } = new();
    }
}
