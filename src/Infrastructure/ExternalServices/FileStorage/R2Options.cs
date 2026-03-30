using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ExternalServices.FileStorage;

public sealed class R2Options
{
    public string AccountId { get; set; } = null!;
    public string AccessKeyId { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;

    // map bucket type -> bucket name
    public Dictionary<string , string> Buckets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
