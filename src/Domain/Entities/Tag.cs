using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("tag")]
[Index("Name", Name = "ux_tags_name", IsUnique = true)]
[Index("Slug", Name = "ux_tags_slug", IsUnique = true)]
public partial class Tag
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = null!;

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("color")]
    public string? Color { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("TagCreatedByNavigations")]
    public virtual User? CreatedByNavigation { get; set; }

    [ForeignKey("UpdatedBy")]
    [InverseProperty("TagUpdatedByNavigations")]
    public virtual User? UpdatedByNavigation { get; set; }

    [ForeignKey("TagId")]
    [InverseProperty("Tags")]
    public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();
}
