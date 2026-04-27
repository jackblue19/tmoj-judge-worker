using MediatR;
using System;

namespace Application.UseCases.StudyPlans.Commands.UpdateStudyPlan;

public class UpdateStudyPlanCommand : IRequest<bool>
{
    public Guid StudyPlanId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public bool IsPaid { get; set; }
    public int Price { get; set; }
    public string? ImageUrl { get; set; }
}
