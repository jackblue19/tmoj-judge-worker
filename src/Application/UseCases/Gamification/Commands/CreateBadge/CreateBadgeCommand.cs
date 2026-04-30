using MediatR;
using Microsoft.AspNetCore.Http;
using System;

namespace Application.UseCases.Gamification.Commands.CreateBadge;

public class CreateBadgeCommand : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string? IconUrl { get; set; }
    public IFormFile? IconFile { get; set; }
    public string? Description { get; set; }
    public string BadgeCode { get; set; } = default!;
    public string BadgeCategory { get; set; } = default!; // contest | course | org | streak | problem
    public int BadgeLevel { get; set; } = 1;
    public bool IsRepeatable { get; set; } = false;
}