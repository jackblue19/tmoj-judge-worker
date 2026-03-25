using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Runtime
{
    public Guid Id { get; set; }

    public string RuntimeName { get; set; } = null!;

    public string? RuntimeVersion { get; set; }

    public string? ImageRef { get; set; }

    public int DefaultTimeLimitMs { get; set; }

    public int DefaultMemoryLimitKb { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<JudgeRun> JudgeRuns { get; set; } = new List<JudgeRun>();

    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
