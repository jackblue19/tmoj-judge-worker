using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("storage_files")]
public partial class StorageFile
{
    [Key]
    [Column("storage_id")]
    public Guid StorageId { get; set; }

    [Column("owner_id")]
    public Guid OwnerId { get; set; }

    [Column("file_type")]
    public string FileType { get; set; } = null!;

    [Column("file_path")]
    public string FilePath { get; set; } = null!;

    [Column("file_size")]
    public long? FileSize { get; set; }

    [Column("hash_checksum")]
    public string? HashChecksum { get; set; }

    [Column("is_private")]
    public bool IsPrivate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [InverseProperty("Storage")]
    public virtual ICollection<Editorial> Editorials { get; set; } = new List<Editorial>();

    [ForeignKey("OwnerId")]
    [InverseProperty("StorageFiles")]
    public virtual User Owner { get; set; } = null!;
}
