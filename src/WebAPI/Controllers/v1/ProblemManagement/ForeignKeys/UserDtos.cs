using System.ComponentModel.DataAnnotations;

namespace WebAPI.Controllers.v1.ProblemManagement.ForeignKeys;  // ảnh hưởng do namespaces

public class UserDtos
{
}

public sealed class UserCreateDto
{
    [Required]
    public string FirstName { get; set; } = null!;

    [Required]
    public string LastName { get; set; } = null!;

    [Required]
    public string Username { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    // test CRUD: optional
    public string? Password { get; set; }

    public string? AvatarUrl { get; set; }
    public string? DisplayName { get; set; }

    public string? LanguagePreference { get; set; } // default 'en' if null/empty
    public bool? Status { get; set; } // default TRUE if null

    public Guid? RoleId { get; set; }
}

public sealed class UserUpdateDto
{
    [Required]
    public Guid UserId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public bool? EmailVerified { get; set; }

    public string? Password { get; set; }
    public string? AvatarUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? LanguagePreference { get; set; }
    public bool? Status { get; set; }

    public Guid? RoleId { get; set; }
}

public sealed class UserResponseDto
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    public bool EmailVerified { get; set; }

    public string? AvatarUrl { get; set; }
    public string? DisplayName { get; set; }
    public string LanguagePreference { get; set; } = null!;
    public bool Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Guid? RoleId { get; set; }
}
