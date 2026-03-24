namespace Application.DTOs.NotificationDTOs;

public class CreateNotificationRequestDto
{
    public Guid UserId { get; set; }       // người nhận

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public string Type { get; set; } = null!;
    // ví dụ: "GRADE", "SYSTEM", "COMMENT"

    public string? ScopeType { get; set; }
    // ví dụ: "Submission", "Course"

    public Guid? ScopeId { get; set; }

    public Guid? CreatedBy { get; set; }
}