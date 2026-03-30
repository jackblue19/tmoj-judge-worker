namespace WebAPI.Controllers.v1.TempStorage;

public class TempStorageDtos
{
}

public class CreateStorageDto
{
    public Guid OwnerId { get; set; }
    public string FileType { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long? FileSize { get; set; }
    public string? HashChecksum { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateStorageDto
{
    public string? FileType { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public bool? IsPrivate { get; set; }
    public DateTime? ExpiresAt { get; set; }
}


public class StorageResponseDto
{
    public Guid StorageId { get; set; }
    public string FileType { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public bool IsPrivate { get; set; }

    public OwnerDto Owner { get; set; } = null!;
}

public class OwnerDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
}
