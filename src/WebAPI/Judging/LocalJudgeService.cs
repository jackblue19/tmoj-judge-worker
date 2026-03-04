using System.Diagnostics;
using System.Text;
using WebAPI.Controllers.v1.SubmissionManagement;

namespace WebAPI.Judging;

public sealed class LocalJudgeService
{
    private const string CppCompiler = "g++";

    public async Task<JudgeManyResponse> CompileAndRunManyAsync(
        CompileRunManyRequest req ,
        CancellationToken ct)
    {
        var workDir = CreateWorkDir();
        try
        {
            var exePath = Path.Combine(workDir , "solution.exe");

            // 1) COMPILE
            var compile = await CompileCppAsync(req.SourceCode , exePath , workDir , ct);
            if ( !compile.Ok )
            {
                return new JudgeManyResponse
                {
                    Compile = compile ,
                    Summary = new RunSummary
                    {
                        Verdict = "ce" ,
                        Passed = 0 ,
                        Total = req.Cases.Count ,
                        TimeMs = 0
                    } ,
                    Cases = new List<JudgeCaseResult>()
                };
            }

            // 2) RUN CASES
            var cases = new List<JudgeCaseResult>(req.Cases.Count);
            var passed = 0;
            var totalTime = 0;

            foreach ( var c in req.Cases.OrderBy(x => x.Ordinal) )
            {
                ct.ThrowIfCancellationRequested();

                var run = await RunExeAsync(
                    exePath ,
                    c.Input ?? string.Empty ,
                    req.TimeLimitMs ,
                    workDir ,
                    ct);

                totalTime += run.ElapsedMs;

                var actual = run.Stdout;
                var expected = c.ExpectedOutput ?? string.Empty;

                var verdict = "ie";

                if ( run.TimedOut )
                {
                    verdict = "tle";
                }
                else if ( run.ExitCode != 0 )
                {
                    verdict = "re";
                }
                else
                {
                    var ok = CompareOutputs(actual , expected , req.CompareMode);
                    verdict = ok ? "ac" : "wa";
                }

                if ( verdict == "ac" ) passed++;

                cases.Add(new JudgeCaseResult
                {
                    TestcaseId = c.TestcaseId ,
                    Ordinal = c.Ordinal ,
                    Verdict = verdict ,
                    ExitCode = run.ExitCode ,
                    TimedOut = run.TimedOut ,
                    TimeMs = run.ElapsedMs ,
                    Stdout = run.Stdout ,
                    Stderr = run.Stderr ,
                    Input = c.Input ,
                    ExpectedOutput = c.ExpectedOutput ,
                    ActualOutput = actual
                });

                if ( req.StopOnFirstFail && verdict != "ac" )
                    break;
            }

            var finalVerdict = cases.Any(x => x.Verdict != "ac") ? cases.First(x => x.Verdict != "ac").Verdict : "ac";

            return new JudgeManyResponse
            {
                Compile = compile ,
                Summary = new RunSummary
                {
                    Verdict = finalVerdict ,
                    Passed = passed ,
                    Total = req.Cases.Count ,
                    TimeMs = totalTime
                } ,
                Cases = cases
            };
        }
        finally
        {
            try { Directory.Delete(workDir , true); } catch { }
        }
    }

    private static string CreateWorkDir()
    {
        var dir = Path.Combine(Path.GetTempPath() , "tmoj_judge_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static bool CompareOutputs(string actual , string expected , CompareMode mode)
    {
        if ( mode == CompareMode.Raw )
            return actual == expected;

        var a = Normalize(actual);
        var e = Normalize(expected);
        return a == e;
    }

    private static string Normalize(string input)
    {
        return input.Replace("\r\n" , "\n")
                    .Replace("\r" , "\n")
                    .TrimEnd();
    }

    private static async Task<CompileInfo> CompileCppAsync(
        string sourceCode ,
        string exePath ,
        string workDir ,
        CancellationToken ct)
    {
        var cppPath = Path.Combine(workDir , "main.cpp");
        await File.WriteAllTextAsync(cppPath , sourceCode , Encoding.UTF8 , ct);

        var psi = new ProcessStartInfo
        {
            FileName = CppCompiler ,
            Arguments = $"-std=c++17 -O2 \"{cppPath}\" -o \"{exePath}\"" ,
            WorkingDirectory = workDir ,
            RedirectStandardOutput = true ,
            RedirectStandardError = true ,
            RedirectStandardInput = false ,
            UseShellExecute = false ,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };

        p.Start();

        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        await p.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new CompileInfo
        {
            Ok = p.ExitCode == 0 ,
            ExitCode = p.ExitCode ,
            Stdout = stdout ?? "" ,
            Stderr = stderr ?? ""
        };
    }

    private sealed class RunInternal
    {
        public int ExitCode { get; set; }
        public bool TimedOut { get; set; }
        public int ElapsedMs { get; set; }
        public string Stdout { get; set; } = "";
        public string Stderr { get; set; } = "";
    }

    private static async Task<RunInternal> RunExeAsync(
        string exePath ,
        string stdin ,
        int timeLimitMs ,
        string workDir ,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exePath ,
            WorkingDirectory = workDir ,
            RedirectStandardInput = true ,
            RedirectStandardOutput = true ,
            RedirectStandardError = true ,
            UseShellExecute = false ,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };

        var sw = Stopwatch.StartNew();
        p.Start();

        // Bắt đầu đọc output ngay lập tức (an toàn hơn BeginOutputReadLine)
        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        // Ghi stdin + đóng để gửi EOF (quan trọng nhất để tránh treo)
        await p.StandardInput.WriteAsync(stdin);
        p.StandardInput.Close();

        var exitTask = p.WaitForExitAsync(ct);
        var delayTask = Task.Delay(timeLimitMs , ct);

        var finished = await Task.WhenAny(exitTask , delayTask);

        if ( finished == delayTask )
        {
            try { p.Kill(entireProcessTree: true); } catch { }
            sw.Stop();

            return new RunInternal
            {
                ExitCode = -1 ,
                TimedOut = true ,
                ElapsedMs = (int) sw.ElapsedMilliseconds ,
                Stdout = "" ,
                Stderr = ""
            };
        }

        await exitTask;
        sw.Stop();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new RunInternal
        {
            ExitCode = p.ExitCode ,
            TimedOut = false ,
            ElapsedMs = (int) sw.ElapsedMilliseconds ,
            Stdout = stdout ?? "" ,
            Stderr = stderr ?? ""
        };
    }
}