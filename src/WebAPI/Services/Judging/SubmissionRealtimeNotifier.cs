using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;
using WebAPI.Models.Submissions;

namespace WebAPI.Services.Judging;

public sealed class SubmissionRealtimeNotifier
{
    private readonly IHubContext<SubmissionHub> _hubContext;

    public SubmissionRealtimeNotifier(IHubContext<SubmissionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySubmissionCompletedAsync(
        SubmissionVerdictEventDto evt ,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(SubmissionHub.GetUserGroup(evt.UserId))
            .SendAsync("submissionCompleted" , evt , ct);
    }

    public async Task NotifySubmissionUpdatedAsync(
        SubmissionVerdictEventDto evt ,
        CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group(SubmissionHub.GetUserGroup(evt.UserId))
            .SendAsync("submissionUpdated" , evt , ct);
    }
}