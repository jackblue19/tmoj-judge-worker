using System;
using System.Collections.Generic;

namespace WebAPI.Controllers.v1.SemesterManagement;

public record SemesterResponse(
    Guid SemesterId,
    string Code,
    string Name,
    DateOnly StartAt,
    DateOnly EndAt,
    bool IsActive,
    DateTime CreatedAt
);

public record SemesterListResponse(
    List<SemesterResponse> Items,
    int TotalCount
);
