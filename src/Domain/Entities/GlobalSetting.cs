using System;

namespace Domain.Entities;

public partial class GlobalSetting
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }
}
