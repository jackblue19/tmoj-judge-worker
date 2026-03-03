using System.Diagnostics;
using System.Text;
using WebAPI.Controllers.v1.SubmissionManagement;

namespace WebAPI.Judging;

public sealed class LocalJudgeService
{
    private readonly string _root;

    public LocalJudgeService(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath , "judge_workspace");
        Directory.CreateDirectory(_root);
    }

    public async Task<RunSampleResponse> CompileAndRunSamplesAsync(CompileRunRequest req , CancellationToken ct)
    {
        var workId = Guid.NewGuid().ToString("N");
        var workDir = Path.Combine(_root , workId);
        Directory.CreateDirectory(workDir);

        try
        {
            var (sourceFileName, compilerExe, compilerArgsTemplate) = ResolveCompiler(req.RuntimeName);

            var srcPath = Path.Combine(workDir , sourceFileName);
            await File.WriteAllTextAsync(srcPath , req.SourceCode , Encoding.UTF8 , ct);

            var exePath = Path.Combine(workDir , "program.exe");

            var compile = await RunProcessAsync(new ProcessSpec
            {
                FileName = compilerExe ,
                Arguments = compilerArgsTemplate
                    .Replace("{src}" , Quote(srcPath))
                    .Replace("{exe}" , Quote(exePath)) ,
                WorkingDirectory = workDir ,
                TimeoutMs = 30_000
            } , ct);

            var response = new RunSampleResponse
            {
                Compile = new CompileInfo
                {
                    Ok = compile.ExitCode == 0 ,
                    ExitCode = compile.ExitCode ,
                    Stdout = compile.Stdout ,
                    Stderr = compile.Stderr
                } ,
                Summary = new RunSummary()
            };

            if ( compile.ExitCode != 0 )
            {
                response.Summary.Verdict = "ce";
                response.Summary.Passed = 0;
                response.Summary.Total = req.Tests.Count;
                return response;
            }

            int passed = 0;
            int total = req.Tests.Count;
            int maxTime = 0;

            foreach ( var t in req.Tests.OrderBy(x => x.Index) )
            {
                var run = await RunProcessAsync(new ProcessSpec
                {
                    FileName = exePath ,
                    Arguments = "" ,
                    WorkingDirectory = workDir ,
                    TimeoutMs = req.TimeLimitMs ,
                    Stdin = t.Input
                } , ct);

                var caseVerdict = "ac";

                if ( run.TimedOut )
                {
                    caseVerdict = "tle";
                }
                else if ( run.ExitCode != 0 )
                {
                    caseVerdict = "re";
                }
                else
                {
                    var actualNormalized = Normalize(run.Stdout , req.CompareMode);
                    var expectedNormalized = Normalize(t.ExpectedOutput , req.CompareMode);

                    if ( !string.Equals(actualNormalized , expectedNormalized , StringComparison.Ordinal) )
                        caseVerdict = "wa";
                }

                if ( caseVerdict == "ac" ) passed++;

                maxTime = Math.Max(maxTime , run.ElapsedMs);

                response.Cases.Add(new SampleCaseResult
                {
                    Index = t.Index ,
                    Verdict = caseVerdict ,
                    ExitCode = run.ExitCode ,
                    TimedOut = run.TimedOut ,
                    TimeMs = run.ElapsedMs ,
                    Stdout = run.Stdout ,
                    Stderr = run.Stderr ,
                    ExpectedOutput = t.ExpectedOutput ,
                    ActualOutput = run.Stdout
                });
            }

            response.Summary.Total = total;
            response.Summary.Passed = passed;
            response.Summary.TimeMs = maxTime;
            response.Summary.Verdict = passed == total ? "ac" : response.Cases.First(x => x.Verdict != "ac").Verdict;

            return response;
        }
        finally
        {
            try { Directory.Delete(workDir , recursive: true); } catch { }
        }
    }

    private static (string SourceFileName, string CompilerExe, string CompilerArgsTemplate) ResolveCompiler(string runtimeName)
    {
        var name = runtimeName.Trim().ToLowerInvariant();

        if ( name.Contains("c++") || name.Contains("g++") || name.Contains("cpp") )
        {
            return ("main.cpp", "g++", "-O2 -std=c++17 {src} -o {exe}");
        }

        if ( name.Contains("c (") || name.Contains("gcc") || name == "c" )
        {
            return ("main.c", "gcc", "-O2 -std=c11 {src} -o {exe}");
        }

        throw new InvalidOperationException($"Unsupported runtime: {runtimeName}");
    }

    private static string Normalize(string s , CompareMode mode)
    {
        var x = s ?? string.Empty;
        x = x.Replace("\r\n" , "\n").Replace("\r" , "\n");

        if ( mode == CompareMode.Exact )
            return x;

        x = x.Trim();

        if ( mode == CompareMode.TrimIgnoreOutputPrefix )
        {
            const string prefix = "OUTPUT:";
            var lines = x.Split('\n');
            if ( lines.Length > 0 )
            {
                var first = lines[0].TrimStart();
                if ( first.StartsWith(prefix , StringComparison.OrdinalIgnoreCase) )
                {
                    lines[0] = first.Substring(prefix.Length).TrimStart();
                    x = string.Join("\n" , lines).Trim();
                }
            }
        }

        return x;
    }

    private static string Quote(string p)
    {
        if ( string.IsNullOrWhiteSpace(p) ) return "\"\"";
        if ( p.Contains('"') ) p = p.Replace("\"" , "\\\"");
        return $"\"{p}\"";
    }

    private sealed class ProcessSpec
    {
        public string FileName { get; set; } = null!;
        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public int TimeoutMs { get; set; }
        public string? Stdin { get; set; }
    }

    private sealed class ProcessResult
    {
        public int ExitCode { get; set; }
        public bool TimedOut { get; set; }
        public int ElapsedMs { get; set; }
        public string Stdout { get; set; } = "";
        public string Stderr { get; set; } = "";
    }

    private static async Task<ProcessResult> RunProcessAsync(ProcessSpec spec , CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = spec.FileName ,
            Arguments = spec.Arguments ,
            WorkingDirectory = spec.WorkingDirectory ,
            RedirectStandardInput = true ,
            RedirectStandardOutput = true ,
            RedirectStandardError = true ,
            UseShellExecute = false ,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi , EnableRaisingEvents = true };

        var sw = Stopwatch.StartNew();
        p.Start();

        if ( !string.IsNullOrEmpty(spec.Stdin) )
        {
            await p.StandardInput.WriteAsync(spec.Stdin);
        }
        p.StandardInput.Close();

        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();

        var timedOut = false;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(spec.TimeoutMs);

        try
        {
            await p.WaitForExitAsync(timeoutCts.Token);
        }
        catch ( OperationCanceledException )
        {
            timedOut = true;
            try { p.Kill(entireProcessTree: true); } catch { }
            try { await p.WaitForExitAsync(CancellationToken.None); } catch { }
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        sw.Stop();

        return new ProcessResult
        {
            ExitCode = timedOut ? -1 : p.ExitCode ,
            TimedOut = timedOut ,
            ElapsedMs = (int) sw.ElapsedMilliseconds ,
            Stdout = stdout ?? "" ,
            Stderr = stderr ?? ""
        };
    }
}
