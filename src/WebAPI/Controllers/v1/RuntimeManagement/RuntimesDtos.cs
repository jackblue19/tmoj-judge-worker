namespace WebAPI.Controllers.v1.RuntimeManagement;

public class RuntimesDtos
{
}

public sealed class RuntimeDto
{
    public Guid Id { get; set; }
    public string RuntimeName { get; set; } = null!;
    public string? RuntimeVersion { get; set; }
    public string? ImageRef { get; set; }
    public int DefaultTimeLimitMs { get; set; }
    public int DefaultMemoryLimitKb { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateRuntimeRequest
{
    public string RuntimeName { get; set; } = null!;
    public string? RuntimeVersion { get; set; }
    public string? ImageRef { get; set; }
    public int DefaultTimeLimitMs { get; set; } = 1000;
    public int DefaultMemoryLimitKb { get; set; } = 262144;
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateRuntimeRequest
{
    public string? RuntimeName { get; set; }
    public string? RuntimeVersion { get; set; }
    public string? ImageRef { get; set; }
    public int? DefaultTimeLimitMs { get; set; }
    public int? DefaultMemoryLimitKb { get; set; }
    public bool? IsActive { get; set; }
}
