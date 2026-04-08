namespace WebAPI.Controllers.v1.ClassSlotManagement;

// ── ClassSlot (Assignment) Requests ───────────────────────

/// <summary>Create a new assignment / slot for a class (Teacher).</summary>
public record CreateClassSlotRequest(
    int SlotNo,
    string Title,
    string? Description,
    string? Rules,
    DateTime? OpenAt,
    DateTime? DueAt,
    DateTime? CloseAt,
    string Mode,               // "problemset" | "contest"
    List<SlotProblemItem>? Problems);

/// <summary>Problem entry in a slot.</summary>
public record SlotProblemItem(
    Guid ProblemId,
    int? Ordinal,
    int? Points,
    bool IsRequired);

/// <summary>Update problem entry in a slot.</summary>
public record UpdateSlotProblemRequest(
    int? Ordinal,
    int? Points,
    bool IsRequired);

/// <summary>Update slot details (Teacher).</summary>
public record UpdateClassSlotRequest(
    string? Title,
    string? Description,
    string? Rules,
    DateTime? OpenAt,
    DateTime? DueAt,
    DateTime? CloseAt,
    bool? IsPublished);

/// <summary>Set or change the due date of a slot (Teacher).</summary>
public record SetDueDateRequest(
    DateTime DueAt,
    DateTime? CloseAt);
