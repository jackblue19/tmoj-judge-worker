using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("collections")]
public partial class Collection
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("type")]
    [StringLength(30)]
    public string Type { get; set; } = null!;

    [Column("is_visibility")]
    public bool IsVisibility { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Collection")]
    public virtual ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();

    [ForeignKey("UserId")]
    [InverseProperty("Collection")]
    public virtual User User { get; set; } = null!;
}
