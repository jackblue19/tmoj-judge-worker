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

        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }

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

    // ✅ EDIT COMMENT
    public class UpdateCommentDto
    {
        public Guid CommentId { get; set; }

        public string Content { get; set; } = string.Empty;
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

        public DateTime? UpdatedAt { get; set; }

        // frontend badge giống LeetCode
        public bool IsEdited =>
            UpdatedAt.HasValue &&
            CreatedAt.HasValue &&
            UpdatedAt.Value != CreatedAt.Value;

        // ✅ READY FOR VOTE SYSTEM
        public int VoteCount { get; set; }

        public int? CurrentUserVote { get; set; }
        // 1 = upvote
        // -1 = downvote
        // null = chưa vote

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
    // CURSOR PAGINATION (🔥 FIX ERROR)
    // =====================================================

    public class CursorPaginationDto<T>
    {
        public List<T> Items { get; set; } = new();

        // ✅ stable cursor
        public DateTime? NextCursorCreatedAt { get; set; }

        public Guid? NextCursorId { get; set; }

        public bool HasMore { get; set; }
    }

   
}