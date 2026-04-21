using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public partial class ProblemTemplate
{
    public Guid CodeTemplateId { get; set; }

    public Guid ProblemId { get; set; }
    public Guid RuntimeId { get; set; }

    public string TemplateCode { get; set; } = null!;
    public string InjectionPoint { get; set; } = "{{USER_CODE}}";

    public string? SolutionSignature { get; set; }

    public string WrapperType { get; set; } = "full";

    public int Version { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    // Navigation
    public virtual Problem Problem { get; set; } = null!;
    public virtual Runtime Runtime { get; set; } = null!;
    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
