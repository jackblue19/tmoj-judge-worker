using System.Text.Json.Serialization;
using MediatR;

namespace Application.UseCases.Contests.Commands;

public class SubmitContestCommand : IRequest<Guid>
{
    [JsonIgnore]
    public Guid ContestId { get; set; }
    public Guid ContestProblemId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = "cpp";

    public Guid? ClassSlotId { get; set; }
}