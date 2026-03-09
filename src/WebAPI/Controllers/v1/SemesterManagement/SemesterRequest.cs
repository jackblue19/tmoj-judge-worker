using System;

namespace WebAPI.Controllers.v1.SemesterManagement;

public record CreateSemesterRequest(
    string Code,
    string Name,
    DateOnly StartAt,
    DateOnly EndAt
);

public record UpdateSemesterRequest(
    string Code,
    string Name,
    DateOnly StartAt,
    DateOnly EndAt,
    bool IsActive
);
