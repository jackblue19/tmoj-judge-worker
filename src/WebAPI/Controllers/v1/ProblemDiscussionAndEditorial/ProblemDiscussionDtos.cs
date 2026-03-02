namespace WebAPI.Controllers.v1.ProblemDiscussionAndEditorial
{
    // =====================================================
    // DISCUSSION
    // =====================================================

    public class CreateDiscussionDto
    {
        public Guid ProblemId { get; set; }
        public Guid UserId { get; set; }

        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    public class UpdateDiscussionDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    public class DiscussionResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProblemId { get; set; }
        public Guid UserId { get; set; }

        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;

        public bool? IsPinned { get; set; }
        public bool? IsLocked { get; set; }

        public DateTime? CreatedAt { get; set; }
    }

    // =====================================================
    // COMMENT
    // =====================================================

    public class CreateCommentDto
    {
        public Guid DiscussionId { get; set; }
        public Guid UserId { get; set; }

        public string Content { get; set; } = null!;

        // null = root comment
        public Guid? ParentId { get; set; }
    }

    // ✅ NEW — EDIT COMMENT
    public class UpdateCommentDto
    {
        public string Content { get; set; } = null!;
    }

    // =====================================================
    // COMMENT TREE RESPONSE
    // =====================================================

    public class CommentTreeDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Content { get; set; } = null!;

        public Guid? ParentId { get; set; }

        public DateTime? CreatedAt { get; set; }

        // ✅ NEW (future: edited badge)
        public DateTime? UpdatedAt { get; set; }

        // ✅ helpful frontend flag
        public bool IsEdited =>
            UpdatedAt != null &&
            UpdatedAt != CreatedAt;

        public List<CommentTreeDto> Replies { get; set; }
            = new();
    }

    // =====================================================
    // DISCUSSION DETAIL
    // =====================================================

    public class DiscussionDetailDto
    {
        public Guid Id { get; set; }

        public Guid ProblemId { get; set; }

        public Guid UserId { get; set; }

        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTime? CreatedAt { get; set; }

        public List<CommentTreeDto> Comments { get; set; }
            = new();
    }

    // =====================================================
    // EDITORIAL
    // =====================================================

    public class CreateEditorialDto
    {
        public Guid ProblemId { get; set; }
        public Guid AuthorId { get; set; }

        public string Content { get; set; } = null!;
    }

    public class UpdateEditorialDto
    {
        public string Content { get; set; } = null!;
    }
}