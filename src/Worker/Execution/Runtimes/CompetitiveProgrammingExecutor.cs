using Contracts.Submissions.Judging;
using Worker.Execution.Containers;
using Worker.Execution.Testset;
using Worker.Execution.Utils;
using Microsoft.Extensions.Logging;

namespace Worker.Execution.Runtimes;

public sealed class CompetitiveProgrammingExecutor : IRuntimeExecutor
{
    private readonly ILogger<CompetitiveProgrammingExecutor> _logger;
    private readonly TestsetEnsureService _ensure;
    private readonly TestsetLayoutAdapter _adapter;
    private readonly DockerSandboxRunner _docker;

    public CompetitiveProgrammingExecutor(
        ILogger<CompetitiveProgrammingExecutor> logger ,
        TestsetEnsureService ensure ,
        TestsetLayoutAdapter adapter ,
        DockerSandboxRunner docker)
    {
        _logger = logger;
        _ensure = ensure;
        _adapter = adapter;
        _docker = docker;
    }

    public bool CanHandle(DispatchJudgeJobContract job)
        => true; // only CP for now

    public async Task<JudgeJobCompletedContract> ExecuteAsync(
        DispatchJudgeJobContract job ,
        CancellationToken ct)
    {
        var profile = RuntimeProfileRegistry.Resolve(job.RuntimeName);

        var workDir = Path.Combine("/var/lib/tmoj/runtime" , $"{job.SubmissionId:N}");
        Directory.CreateDirectory(workDir);

        try
        {
            // 1. ensure testset
            await _ensure.EnsureAsync(job.ProblemSlug , job.TestsetId , job.ProblemId , ct);

            // 2. write source
            var sourcePath = Path.Combine(workDir , profile.SourceFileName);
            await File.WriteAllTextAsync(sourcePath , job.SourceCode , ct);

            // 3. compile (if needed)
            if ( profile.HasCompileStep )
            {
                var compile = await _docker.RunAsync(new DockerRunRequest
                {
                    Image = "vnoj/judge-tiericpc:amd64-latest" ,
                    WorkingDirectory = "/work" ,
                    Mounts = new()
                    {
                        new() { HostPath = workDir, ContainerPath = "/work" }
                    } ,
                    Command = $"sh -lc \"{profile.CompileCommand}\"" ,
                    TimeoutMs = job.TimeLimitMs * 2
                } , ct);

                if ( compile.ExitCode != 0 )
                {
                    return BuildCompileError(job , compile);
                }
            }

            var results = new List<JudgeCaseCompletedContract>();
            int passed = 0;

            // 4. run testcases
            foreach ( var c in job.Cases.OrderBy(x => x.Ordinal) )
            {
                var prepared = await _adapter.PrepareCaseAsync(
                    job.ProblemSlug ,
                    job.TestsetId ,
                    c ,
                    workDir ,
                    ct);

                var outputPath = Path.Combine(prepared.CaseDirectory , "actual.txt");

                var run = await _docker.RunAsync(new DockerRunRequest
                {
                    Image = "vnoj/judge-tiericpc:amd64-latest" ,
                    WorkingDirectory = "/work" ,
                    Mounts = new()
                    {
                        new() { HostPath = workDir, ContainerPath = "/work" }
                    } ,
                    Command =
                        $"sh -lc \"{profile.RunCommand} < {prepared.InputPath} > {outputPath}\"" ,
                    TimeoutMs = job.TimeLimitMs
                } , ct);

                string actual = File.Exists(outputPath)
                    ? await File.ReadAllTextAsync(outputPath , ct)
                    : "";

                string expected = await File.ReadAllTextAsync(prepared.ExpectedPath , ct);

                string verdict;

                if ( run.TimedOut ) verdict = "tle";
                else if ( run.ExitCode != 0 ) verdict = "re";
                else if ( OutputComparer.Compare(expected , actual) ) verdict = "ac";
                else verdict = "wa";

                if ( verdict == "ac" ) passed++;

                results.Add(new JudgeCaseCompletedContract
                {
                    TestcaseId = c.TestcaseId ,
                    Ordinal = c.Ordinal ,
                    Verdict = verdict ,
                    ExitCode = run.ExitCode ,
                    TimedOut = run.TimedOut ,
                    TimeMs = run.ElapsedMs ,
                    MemoryKb = null ,
                    ActualOutput = actual ,
                    ExpectedOutput = null
                });

                if ( job.StopOnFirstFail && verdict != "ac" )
                    break;
            }

            var finalVerdict = results.All(x => x.Verdict == "ac") ? "ac" : results.First(x => x.Verdict != "ac").Verdict;

            return new JudgeJobCompletedContract
            {
                JobId = job.JobId ,
                JudgeRunId = job.JudgeRunId ,
                SubmissionId = job.SubmissionId ,
                WorkerId = job.WorkerId ,
                Status = "done" ,
                Compile = new() { Ok = true , ExitCode = 0 } ,
                Summary = new()
                {
                    Verdict = finalVerdict ,
                    Passed = passed ,
                    Total = job.Cases.Count ,
                    TimeMs = results.Max(x => x.TimeMs) ,
                    FinalScore = (decimal) passed / job.Cases.Count * 100
                } ,
                Cases = results
            };
        }
        catch ( Exception ex )
        {
            return new JudgeJobCompletedContract
            {
                JobId = job.JobId ,
                JudgeRunId = job.JudgeRunId ,
                SubmissionId = job.SubmissionId ,
                WorkerId = job.WorkerId ,
                Status = "failed" ,
                Note = ex.Message ,
                Compile = new() { Ok = false } ,
                Summary = new() { Verdict = "ie" }
            };
        }
        finally
        {
            try { Directory.Delete(workDir , true); } catch { }
        }
    }

    private static JudgeJobCompletedContract BuildCompileError(
        DispatchJudgeJobContract job ,
        DockerRunResult compile)
    {
        return new JudgeJobCompletedContract
        {
            JobId = job.JobId ,
            JudgeRunId = job.JudgeRunId ,
            SubmissionId = job.SubmissionId ,
            WorkerId = job.WorkerId ,
            Status = "compile_error" ,
            Compile = new()
            {
                Ok = false ,
                ExitCode = compile.ExitCode ,
                Stdout = compile.Stdout ,
                Stderr = compile.Stderr
            } ,
            Summary = new()
            {
                Verdict = "ce" ,
                Passed = 0 ,
                Total = job.Cases.Count
            }
        };
    }
}