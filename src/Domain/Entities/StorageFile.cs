using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class StorageFile
{
    public Guid StorageId { get; set; }

    public Guid OwnerId { get; set; }

    public string FileType { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public long? FileSize { get; set; }

    public string? HashChecksum { get; set; }

    public bool IsPrivate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public virtual ICollection<Editorial> Editorials { get; set; } = new List<Editorial>();

    public virtual User Owner { get; set; } = null!;
}
