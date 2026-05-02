using Contracts.Submissions.Judging;

namespace Worker.Services;

public sealed class SubmissionJobProcessor
{
    private readonly JudgeBackendClient _backend;
    private readonly LocalJudgeService _judge;

    public SubmissionJobProcessor(
        JudgeBackendClient backend ,
        LocalJudgeService judge)
    {
        _backend = backend;
        _judge = judge;
    }

    public async Task ProcessOneAsync(Guid workerId , CancellationToken ct)
    {
        var job = await _backend.ClaimNextAsync(workerId , ct);
        if ( job is null )
            return;

        try
        {
            var compile = await _judge.CompileAsync(job , ct);

            if ( !compile.Ok )
            {
                var completed = new JudgeJobCompletedContract
                {
                    JobId = job.JobId ,
                    JudgeRunId = job.JudgeRunId ,
                    SubmissionId = job.SubmissionId ,
                    WorkerId = workerId ,
                    Status = "failed" ,
                    Note = "Compilation failed." ,
                    Compile = new JudgeCompileResultContract
                    {
                        Ok = false ,
                        ExitCode = compile.ExitCode ,
                        TimeMs = compile.TimeMs ,
                        Stdout = compile.Stdout ?? "" ,
                        Stderr = compile.Stderr ?? ""
                    } ,
                    Summary = new JudgeSummaryResultContract
                    {
                        Verdict = "ce" ,
                        Passed = 0 ,
                        Total = job.Cases.Count ,
                        TimeMs = compile.TimeMs ,
                        MemoryKb = null ,
                        FinalScore = 0
                    } ,
                    Cases = new List<JudgeCaseCompletedContract>()
                };

                await _backend.CompleteAsync(completed , ct);
                return;
            }

            var caseResults = await _judge.RunCasesAsync(job , compile , ct);

            var passed = caseResults.Count(x => x.Verdict == "ac");
            var finalVerdict = BuildFinalVerdict(caseResults);
            var finalScore = CalculateScore(caseResults , job.Cases);
            var timeMs = caseResults.Count == 0 ? 0 : caseResults.Max(x => x.TimeMs ?? 0);
            //var memoryKb = caseResults.Count == 0 ? 0 : caseResults.Max(x => x.MemoryKb ?? 0);
            var memoryKbValue = caseResults
                            .Where(x => x.MemoryKb.HasValue && x.MemoryKb.Value > 0)
                            .Select(x => x.MemoryKb!.Value)
                            .DefaultIfEmpty(0)
                            .Max();
            int? memoryKb = memoryKbValue > 0 ? memoryKbValue : null;

            var completedOk = new JudgeJobCompletedContract
            {
                JobId = job.JobId ,
                JudgeRunId = job.JudgeRunId ,
                SubmissionId = job.SubmissionId ,
                WorkerId = workerId ,
                Status = "done" ,
                Note = null ,
                Compile = new JudgeCompileResultContract
                {
                    Ok = true ,
                    ExitCode = compile.ExitCode ,
                    TimeMs = compile.TimeMs ,
                    Stdout = compile.Stdout ?? "" ,
                    Stderr = compile.Stderr ?? ""
                } ,
                Summary = new JudgeSummaryResultContract
                {
                    Verdict = finalVerdict ,
                    Passed = passed ,
                    Total = job.Cases.Count ,
                    TimeMs = timeMs ,
                    MemoryKb = memoryKb ,
                    FinalScore = finalScore
                } ,
                Cases = caseResults
            };

            await _backend.CompleteAsync(completedOk , ct);
        }
        catch ( Exception ex )
        {
            var failed = new JudgeJobCompletedContract
            {
                JobId = job.JobId ,
                JudgeRunId = job.JudgeRunId ,
                SubmissionId = job.SubmissionId ,
                WorkerId = workerId ,
                Status = "failed" ,
                Note = ex.ToString() ,
                Compile = new JudgeCompileResultContract
                {
                    Ok = false ,
                    ExitCode = -1 ,
                    TimeMs = null ,
                    Stdout = "" ,
                    Stderr = ""
                } ,
                Summary = new JudgeSummaryResultContract
                {
                    Verdict = "ie" ,
                    Passed = 0 ,
                    Total = job.Cases.Count ,
                    TimeMs = null ,
                    MemoryKb = null ,
                    FinalScore = 0
                } ,
                Cases = new List<JudgeCaseCompletedContract>()
            };

            await _backend.CompleteAsync(failed , ct);
        }
    }

    private static string BuildFinalVerdict(IReadOnlyList<JudgeCaseCompletedContract> cases)
    {
        if ( cases.Count == 0 ) return "ie";
        if ( cases.All(x => x.Verdict == "ac") ) return "ac";
        if ( cases.Any(x => x.Verdict == "wa") ) return "wa";
        if ( cases.Any(x => x.Verdict == "tle") ) return "tle";
        if ( cases.Any(x => x.Verdict == "mle") ) return "mle";
        if ( cases.Any(x => x.Verdict == "re") ) return "re";
        return "ie";
    }

    private static decimal CalculateScore(
        IReadOnlyList<JudgeCaseCompletedContract> caseResults ,
        IReadOnlyList<DispatchJudgeCaseContract> cases)
    {
        var weightMap = cases.ToDictionary(x => x.TestcaseId , x => x.Weight);
        decimal totalWeight = cases.Sum(x => x.Weight);
        if ( totalWeight <= 0 ) return 0;

        decimal earned = 0;
        foreach ( var r in caseResults )
        {
            if ( r.Verdict == "ac" && weightMap.TryGetValue(r.TestcaseId , out var w) )
                earned += w;
        }

        return Math.Round((earned / totalWeight) * 100m , 2);
    }
}