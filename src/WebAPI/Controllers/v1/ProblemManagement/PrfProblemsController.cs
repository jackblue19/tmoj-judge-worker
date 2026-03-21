using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace WebAPI.Controllers.v1.ProblemManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PrfProblemsController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly IWebHostEnvironment _env;

    public PrfProblemsController(TmojDbContext db , IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ============================================================
    // 1) UPLOAD PRF192 PACKAGE (zip)
    // POST api/v1/PrfProblems/{problemId}/prf192/upload
    // form-data: package=<zip>
    public sealed class Prf192UploadRequestDto
    {
        public IFormFile Package { get; set; } = default!;
    }
    // ============================================================
    [HttpPost("{problemId:guid}/prf192/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PrfUploadResponseDto>> UploadPrf192Package(
        [FromRoute] Guid problemId ,
        [FromForm] Prf192UploadRequestDto form ,
        CancellationToken ct)
    {
        var package = form.Package;

        if ( package is null || package.Length == 0 )
            return BadRequest(new { message = "package is required" });

        var problemExists = await _db.Problems.AnyAsync(x => x.Id == problemId , ct);
        if ( !problemExists )
            return NotFound(new { message = "problem not found" });

        var workRoot = Path.Combine(_env.ContentRootPath , "App_Data" , "prf192" , "import");
        Directory.CreateDirectory(workRoot);

        var importId = Guid.NewGuid().ToString("N");
        var zipPath = Path.Combine(workRoot , $"{importId}.zip");
        var extractDir = Path.Combine(workRoot , importId);

        await using ( var fs = System.IO.File.Create(zipPath) )
        {
            await package.CopyToAsync(fs , ct);
        }

        if ( Directory.Exists(extractDir) ) Directory.Delete(extractDir , true);
        Directory.CreateDirectory(extractDir);

        try
        {
            ZipFile.ExtractToDirectory(zipPath , extractDir);

            var folder1 = FindFolderNamed(extractDir , "1");
            if ( folder1 is null )
                return BadRequest(new { message = "Invalid package: missing folder '1'." });

            var givenDir = Path.Combine(folder1 , "Given");
            var tcDir = Path.Combine(folder1 , "TestCases");
            if ( !Directory.Exists(givenDir) || !Directory.Exists(tcDir) )
                return BadRequest(new { message = "Invalid package: missing Given/ or TestCases/ under folder '1'." });

            var templateCPath = Directory.EnumerateFiles(givenDir , "*.c" , SearchOption.TopDirectoryOnly).FirstOrDefault();
            if ( templateCPath is null )
                return BadRequest(new { message = "Invalid package: missing Given/*.c template (e.g., Q1.c)." });

            var docxPath = Directory.EnumerateFiles(folder1 , "*.docx" , SearchOption.TopDirectoryOnly).FirstOrDefault();

            var tcFiles = Directory.EnumerateFiles(tcDir , "*.txt" , SearchOption.TopDirectoryOnly)
                .OrderBy(x => x , StringComparer.OrdinalIgnoreCase)
                .ToList();

            if ( tcFiles.Count == 0 )
                return BadRequest(new { message = "Invalid package: no testcase .txt files found in TestCases/." });

            var testset = new Testset
            {
                Id = Guid.NewGuid() ,
                ProblemId = problemId ,
                Type = "prf192" ,
                IsActive = true ,
                Note = "PRF192" ,
                CreatedAt = DateTime.Now ,//utc ?
            };

            _db.Testsets.Add(testset);

            var ordinal = 1;
            foreach ( var file in tcFiles )
            {
                var raw = await System.IO.File.ReadAllTextAsync(file , ct);
                var parsed = Prf192Parser.Parse(raw);

                var expectedWithMeta = BuildExpectedWithMeta(parsed.ExpectedOutput , parsed.RemoveSpaces , parsed.CaseSensitive , parsed.Mark);

                var tc = new Testcase
                {
                    Id = Guid.NewGuid() ,
                    TestsetId = testset.Id ,
                    Ordinal = ordinal++ ,
                    Weight = Math.Max(1 , (int) Math.Round(parsed.Mark * 1000m)) , // store MARK in weight*1000 (quick hack)
                    IsSample = false ,
                    Input = parsed.Input ,
                    ExpectedOutput = expectedWithMeta
                };

                _db.Testcases.Add(tc);
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new PrfUploadResponseDto
            {
                ProblemId = problemId ,
                TestsetId = testset.Id ,
                Note = testset.Note ?? "PRF192" ,
                TestcaseCount = tcFiles.Count ,
                TemplateFileName = Path.GetFileName(templateCPath) ,
                StatementFileName = docxPath is null ? null : Path.GetFileName(docxPath)
            });
        }
        finally
        {
            TryDeleteFile(zipPath);
            TryDeleteDirectory(extractDir);
        }
    }

    // ============================================================
    // 2) VIEW TESTCASES (with preview + parsed PRF meta)
    // GET api/v1/PrfProblems/{problemId}/testsets/{testsetId}/testcases
    // ============================================================
    [HttpGet("{problemId:guid}/testsets/{testsetId:guid}/testcases")]
    public async Task<ActionResult<PrfTestcaseListDto>> GetTestcases(
        [FromRoute] Guid problemId ,
        [FromRoute] Guid testsetId ,
        CancellationToken ct)
    {
        var testset = await _db.Testsets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == testsetId && x.ProblemId == problemId , ct);

        if ( testset is null ) return NotFound(new { message = "testset not found" });

        var items = await _db.Testcases
            .AsNoTracking()
            .Where(x => x.TestsetId == testsetId)
            .OrderBy(x => x.Ordinal)
            .Select(x => new { x.Id , x.Ordinal , x.Input , x.ExpectedOutput , x.Weight })
            .ToListAsync(ct);

        var dto = new PrfTestcaseListDto
        {
            ProblemId = problemId ,
            TestsetId = testsetId ,
            Note = testset.Note ?? "" ,
            Items = items.Select(x =>
            {
                var meta = Prf192Parser.TryExtractMeta(x.ExpectedOutput ?? "");
                var expectedPure = Prf192Parser.StripMeta(x.ExpectedOutput ?? "");

                return new PrfTestcaseItemDto
                {
                    Id = x.Id ,
                    Ordinal = x.Ordinal ,
                    FileName = $"tc{x.Ordinal}.txt" ,
                    Mark = meta?.Mark ?? (x.Weight / 1000m) ,
                    RemoveSpaces = meta?.RemoveSpaces ?? true ,
                    CaseSensitive = meta?.CaseSensitive ?? false ,
                    InputPreview = x.Input ,
                    OutputPreview = expectedPure
                };
            }).ToList()
        };

        return Ok(dto);
    }

    // ============================================================
    // 3) SUBMIT + JUDGE PRF192 (compile C + run all testcases)
    // POST api/v1/PrfProblems/prf192/submit
    // ============================================================
    [HttpPost("prf192/submit")]
    public async Task<ActionResult<PrfSubmitResponseDto>> SubmitAndJudgePrf192(
        [FromBody] PrfSubmitRequestDto req ,
        CancellationToken ct)
    {
        if ( req.ProblemId == Guid.Empty || req.TestsetId == Guid.Empty )
            return BadRequest(new { message = "problemId and testsetId are required" });

        if ( string.IsNullOrWhiteSpace(req.SourceCode) )
            return BadRequest(new { message = "sourceCode is required" });

        var testset = await _db.Testsets.FirstOrDefaultAsync(x => x.Id == req.TestsetId && x.ProblemId == req.ProblemId , ct);
        if ( testset is null ) return NotFound(new { message = "testset not found" });

        if ( !string.Equals(testset.Note , "PRF192" , StringComparison.OrdinalIgnoreCase) )
            return BadRequest(new { message = "This endpoint only supports testset.note = PRF192" });

        var tcs = await _db.Testcases
            .AsNoTracking()
            .Where(x => x.TestsetId == req.TestsetId)
            .OrderBy(x => x.Ordinal)
            .Select(x => new { x.Id , x.Ordinal , x.Input , x.ExpectedOutput , x.Weight })
            .ToListAsync(ct);

        if ( tcs.Count == 0 ) return BadRequest(new { message = "testset has no testcases" });

        var runRoot = Path.Combine(_env.ContentRootPath , "App_Data" , "prf192" , "runs");
        Directory.CreateDirectory(runRoot);

        var runId = Guid.NewGuid().ToString("N");
        var workDir = Path.Combine(runRoot , runId);
        Directory.CreateDirectory(workDir);

        var sourcePath = Path.Combine(workDir , "main.c");
        await System.IO.File.WriteAllTextAsync(sourcePath , req.SourceCode , new UTF8Encoding(false) , ct);

        var exePath = Path.Combine(workDir , OperatingSystem.IsWindows() ? "main.exe" : "main");

        var compile = await CompileCAsync(
            gccExe: req.CompilerPath ?? "gcc" ,
            sourcePath: sourcePath ,
            outputExePath: exePath ,
            workDir: workDir ,
            timeoutMs: req.CompileTimeoutMs <= 0 ? 8000 : req.CompileTimeoutMs ,
            ct: ct);

        if ( !compile.Success )
        {
            TryDeleteDirectory(workDir);

            var submissionCe = await InsertSubmissionAsync(req , verdictCode: "ce" , finalScore: 0m , timeMs: null , memoryKb: null , ct: ct);
            return Ok(new PrfSubmitResponseDto
            {
                SubmissionId = submissionCe ,
                Verdict = "CE" ,
                TotalScore = 0m ,
                MaxScore = tcs.Sum(x => Prf192Parser.TryExtractMeta(x.ExpectedOutput ?? "")?.Mark ?? (x.Weight / 1000m)) ,
                Compile = new PrfCompileDto { Success = false , ExitCode = compile.ExitCode , StdErr = compile.StdErr , StdOut = compile.StdOut } ,
                Cases = new List<PrfCaseResultDto>()
            });
        }

        var perCase = new List<PrfCaseResultDto>();
        decimal total = 0m;
        decimal max = 0m;
        var overallVerdict = "AC";

        foreach ( var tcRow in tcs )
        {
            var meta = Prf192Parser.TryExtractMeta(tcRow.ExpectedOutput ?? "");
            var removeSpaces = meta?.RemoveSpaces ?? true;
            var caseSensitive = meta?.CaseSensitive ?? false;
            var mark = meta?.Mark ?? (tcRow.Weight / 1000m);
            var expectedPure = Prf192Parser.StripMeta(tcRow.ExpectedOutput ?? "");

            max += mark;

            var exec = await RunExeAsync(
                exePath: exePath ,
                stdin: (tcRow.Input ?? "") + "\n" ,
                workDir: workDir ,
                timeoutMs: req.RunTimeoutMs <= 0 ? 2000 : req.RunTimeoutMs ,
                ct: ct);

            if ( !exec.Success && exec.TimedOut )
            {
                overallVerdict = "TLE";
                perCase.Add(new PrfCaseResultDto
                {
                    Ordinal = tcRow.Ordinal ,
                    Verdict = "TLE" ,
                    Score = 0m ,
                    Mark = mark ,
                    ExpectedPreview = expectedPure ,
                    ActualPreview = Truncate(exec.StdOut , 2000)
                });
                continue;
            }

            if ( !exec.Success )
            {
                overallVerdict = "RE";
                perCase.Add(new PrfCaseResultDto
                {
                    Ordinal = tcRow.Ordinal ,
                    Verdict = "RE" ,
                    Score = 0m ,
                    Mark = mark ,
                    ExpectedPreview = expectedPure ,
                    ActualPreview = Truncate(exec.StdOut , 2000)
                });
                continue;
            }

            var ok = Prf192Parser.AreEqual(exec.StdOut ?? "" , expectedPure , removeSpaces , caseSensitive);
            if ( ok )
            {
                total += mark;
                perCase.Add(new PrfCaseResultDto
                {
                    Ordinal = tcRow.Ordinal ,
                    Verdict = "AC" ,
                    Score = mark ,
                    Mark = mark
                });
            }
            else
            {
                if ( overallVerdict == "AC" ) overallVerdict = "WA";
                perCase.Add(new PrfCaseResultDto
                {
                    Ordinal = tcRow.Ordinal ,
                    Verdict = "WA" ,
                    Score = 0m ,
                    Mark = mark ,
                    ExpectedPreview = Truncate(expectedPure , 2000) ,
                    ActualPreview = Truncate(exec.StdOut , 2000)
                });
            }
        }

        TryDeleteDirectory(workDir);

        var verdictCode = overallVerdict.ToLowerInvariant();
        if ( verdictCode is not ("ac" or "wa" or "tle" or "re") ) verdictCode = "wa";

        var submissionId = await InsertSubmissionAsync(
            req ,
            verdictCode: verdictCode ,
            finalScore: Math.Round(total , 2) ,
            timeMs: null ,
            memoryKb: null ,
            ct: ct);

        return Ok(new PrfSubmitResponseDto
        {
            SubmissionId = submissionId ,
            Verdict = overallVerdict ,
            TotalScore = Math.Round(total , 3) ,
            MaxScore = Math.Round(max , 3) ,
            Compile = new PrfCompileDto { Success = true , ExitCode = compile.ExitCode , StdErr = compile.StdErr , StdOut = compile.StdOut } ,
            Cases = perCase
        });
    }

    // ============================
    // DB insert submission (quick)
    // ============================
    private async Task<Guid> InsertSubmissionAsync(
        PrfSubmitRequestDto req ,
        string verdictCode ,
        decimal finalScore ,
        int? timeMs ,
        int? memoryKb ,
        CancellationToken ct)
    {
        var userId = req.UserId == Guid.Empty
            ? await _db.Users.Select(x => x.UserId).FirstOrDefaultAsync(ct)
            : req.UserId;

        if ( userId == Guid.Empty )
            throw new InvalidOperationException("No user exists in DB. Provide userId in request.");

        var codeBytes = Encoding.UTF8.GetBytes(req.SourceCode ?? "");
        var codeHash = Convert.ToHexString(SHA256.HashData(codeBytes)).ToLowerInvariant();

        var sub = new Submission
        {
            Id = Guid.NewGuid() ,
            UserId = userId ,
            ProblemId = req.ProblemId ,
            CodeSize = codeBytes.Length ,
            CodeHash = codeHash ,
            StatusCode = "done" ,
            VerdictCode = verdictCode ,
            FinalScore = finalScore ,
            TimeMs = timeMs ,
            MemoryKb = memoryKb ,
            JudgedAt = DateTime.Now ,  // utc ?
            TestsetId = req.TestsetId ,
            SubmissionType = "practice" ,
            Type = "prf192" ,
            CustomInput = null
        };

        _db.Submissions.Add(sub);
        await _db.SaveChangesAsync(ct);
        return sub.Id;
    }

    // ============================
    // Compile C
    // ============================
    private static async Task<ProcResult> CompileCAsync(
        string gccExe ,
        string sourcePath ,
        string outputExePath ,
        string workDir ,
        int timeoutMs ,
        CancellationToken ct)
    {
        var args = OperatingSystem.IsWindows()
            ? $"-O2 -std=c11 \"{sourcePath}\" -o \"{outputExePath}\""
            : $"-O2 -std=c11 \"{sourcePath}\" -o \"{outputExePath}\"";

        return await RunProcessAsync(gccExe , args , workDir , stdin: null , timeoutMs , ct);
    }

    // ============================
    // Run exe with stdin
    // ============================
    private static async Task<ProcResult> RunExeAsync(
        string exePath ,
        string stdin ,
        string workDir ,
        int timeoutMs ,
        CancellationToken ct)
    {
        if ( OperatingSystem.IsWindows() )
            return await RunProcessAsync(exePath , "" , workDir , stdin , timeoutMs , ct);

        return await RunProcessAsync(exePath , "" , workDir , stdin , timeoutMs , ct);
    }

    private static async Task<ProcResult> RunProcessAsync(
        string fileName ,
        string args ,
        string workDir ,
        string? stdin ,
        int timeoutMs ,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName ,
            Arguments = args ,
            WorkingDirectory = workDir ,
            RedirectStandardInput = true ,
            RedirectStandardOutput = true ,
            RedirectStandardError = true ,
            UseShellExecute = false ,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };

        try
        {
            p.Start();

            if ( !string.IsNullOrEmpty(stdin) )
            {
                await p.StandardInput.WriteAsync(stdin);
                await p.StandardInput.FlushAsync();
                p.StandardInput.Close();
            }

            var stdOutTask = p.StandardOutput.ReadToEndAsync();
            var stdErrTask = p.StandardError.ReadToEndAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            try
            {
                await p.WaitForExitAsync(cts.Token);
            }
            catch ( OperationCanceledException )
            {
                TryKill(p);
                var so = await SafeGet(stdOutTask);
                var se = await SafeGet(stdErrTask);
                return new ProcResult(false , TimedOut: true , ExitCode: null , StdOut: so , StdErr: se);
            }

            var stdOut = await stdOutTask;
            var stdErr = await stdErrTask;

            var ok = p.ExitCode == 0;
            return new ProcResult(ok , TimedOut: false , ExitCode: p.ExitCode , StdOut: stdOut , StdErr: stdErr);
        }
        catch ( Exception ex )
        {
            return new ProcResult(false , TimedOut: false , ExitCode: null , StdOut: "" , StdErr: ex.Message);
        }
    }

    private static void TryKill(Process p)
    {
        try { if ( !p.HasExited ) p.Kill(entireProcessTree: true); } catch { }
    }

    private static async Task<string> SafeGet(Task<string> t)
    {
        try { return await t; } catch { return ""; }
    }

    private static string? FindFolderNamed(string root , string name)
    {
        foreach ( var d in Directory.EnumerateDirectories(root , "*" , SearchOption.AllDirectories) )
        {
            if ( string.Equals(Path.GetFileName(d) , name , StringComparison.OrdinalIgnoreCase) )
                return d;
        }
        return null;
    }

    private static void TryDeleteFile(string path)
    {
        try { if ( System.IO.File.Exists(path) ) System.IO.File.Delete(path); } catch { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if ( Directory.Exists(path) ) Directory.Delete(path , true); } catch { }
    }

    private static string Truncate(string? s , int max)
    {
        s ??= "";
        if ( s.Length <= max ) return s;
        return s.Substring(0 , max);
    }

    // ============================================================
    // PRF192 Parser + Meta packing into expected_output
    // ============================================================
    private const string MetaMarker = "\n<<PRF192_META>>\n";

    private static string BuildExpectedWithMeta(string expected , bool removeSpaces , bool caseSensitive , decimal mark)
    {
        var sb = new StringBuilder();
        sb.Append(expected ?? "");
        sb.Append(MetaMarker);
        sb.Append("REMOVE_SPACES=").Append(removeSpaces ? "YES" : "NO").Append('\n');
        sb.Append("CASE_SENSITIVE=").Append(caseSensitive ? "YES" : "NO").Append('\n');
        sb.Append("MARK=").Append(mark.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        return sb.ToString();
    }

    private sealed record PrfParsed(string Input , string ExpectedOutput , bool RemoveSpaces , bool CaseSensitive , decimal Mark);

    private sealed record PrfMeta(bool RemoveSpaces , bool CaseSensitive , decimal Mark);

    private static class Prf192Parser
    {
        public static PrfParsed Parse(string raw)
        {
            var lines = SplitLines(raw);

            int iOutput = IndexOfMarker(lines , "OUTPUT:");
            int iRemove = IndexOfMarker(lines , "REMOVE_SPACES:");
            int iCase = IndexOfMarker(lines , "CASE_SENSITIVE:");
            int iMark = IndexOfMarker(lines , "MARK:");

            if ( iOutput < 0 ) throw new FormatException("Missing OUTPUT:");
            if ( iRemove < 0 || iCase < 0 || iMark < 0 ) throw new FormatException("Missing REMOVE_SPACES / CASE_SENSITIVE / MARK");

            var inputLines = lines.Take(iOutput);
            var expectedLines = lines.Skip(iOutput + 1).Take(iRemove - (iOutput + 1));

            var removeSpaces = ParseYesNo(ReadValue(lines , iRemove , "REMOVE_SPACES:"));
            var caseSensitive = ParseYesNo(ReadValue(lines , iCase , "CASE_SENSITIVE:"));
            var mark = decimal.Parse(ReadValue(lines , iMark , "MARK:") , System.Globalization.CultureInfo.InvariantCulture);

            return new PrfParsed(
                Input: JoinLines(inputLines) ,
                ExpectedOutput: JoinLines(expectedLines) ,
                RemoveSpaces: removeSpaces ,
                CaseSensitive: caseSensitive ,
                Mark: mark
            );
        }

        public static bool AreEqual(string actual , string expected , bool removeSpaces , bool caseSensitive)
        {
            var a = Normalize(actual ?? "" , removeSpaces , caseSensitive);
            var e = Normalize(expected ?? "" , removeSpaces , caseSensitive);
            return string.Equals(a , e , StringComparison.Ordinal);
        }

        public static PrfMeta? TryExtractMeta(string expectedWithMeta)
        {
            var idx = expectedWithMeta.IndexOf(MetaMarker , StringComparison.Ordinal);
            if ( idx < 0 ) return null;

            var metaPart = expectedWithMeta.Substring(idx + MetaMarker.Length);
            var metaLines = SplitLines(metaPart);

            bool? remove = null;
            bool? cs = null;
            decimal? mark = null;

            foreach ( var l in metaLines )
            {
                var line = l.Trim();
                if ( line.StartsWith("REMOVE_SPACES=" , StringComparison.OrdinalIgnoreCase) )
                    remove = line.EndsWith("YES" , StringComparison.OrdinalIgnoreCase);
                else if ( line.StartsWith("CASE_SENSITIVE=" , StringComparison.OrdinalIgnoreCase) )
                    cs = line.EndsWith("YES" , StringComparison.OrdinalIgnoreCase);
                else if ( line.StartsWith("MARK=" , StringComparison.OrdinalIgnoreCase) )
                {
                    var v = line.Substring("MARK=".Length).Trim();
                    if ( decimal.TryParse(v , System.Globalization.NumberStyles.Any , System.Globalization.CultureInfo.InvariantCulture , out var m) )
                        mark = m;
                }
            }

            if ( remove is null || cs is null || mark is null ) return null;
            return new PrfMeta(remove.Value , cs.Value , mark.Value);
        }

        public static string StripMeta(string expectedWithMeta)
        {
            var idx = expectedWithMeta.IndexOf(MetaMarker , StringComparison.Ordinal);
            if ( idx < 0 ) return expectedWithMeta;
            return expectedWithMeta.Substring(0 , idx);
        }

        private static string Normalize(string s , bool removeSpaces , bool caseSensitive)
        {
            var lines = SplitLines(s).Select(x => x.TrimEnd()).ToList();
            var joined = JoinLines(lines);

            if ( removeSpaces )
                joined = joined.Replace(" " , "").Replace("\t" , "");

            if ( !caseSensitive )
                joined = joined.ToLowerInvariant();

            return joined.TrimEnd('\r' , '\n');
        }

        private static List<string> SplitLines(string s)
        {
            s = (s ?? "").Replace("\r\n" , "\n").Replace("\r" , "\n");
            return s.Split('\n').ToList();
        }

        private static string JoinLines(IEnumerable<string> lines) => string.Join("\n" , lines);

        private static int IndexOfMarker(List<string> lines , string marker)
        {
            for ( int i = 0; i < lines.Count; i++ )
            {
                if ( lines[i].TrimStart().StartsWith(marker , StringComparison.OrdinalIgnoreCase) )
                    return i;
            }
            return -1;
        }

        private static string ReadValue(List<string> lines , int markerIndex , string marker)
        {
            var line = lines[markerIndex].Trim();
            var after = line.Substring(marker.Length).Trim();
            if ( !string.IsNullOrEmpty(after) ) return after;

            if ( markerIndex + 1 >= lines.Count )
                throw new FormatException($"Missing value for {marker}");

            return lines[markerIndex + 1].Trim();
        }

        private static bool ParseYesNo(string v)
        {
            v = (v ?? "").Trim().ToUpperInvariant();
            return v switch
            {
                "YES" => true,
                "NO" => false,
                _ => throw new FormatException($"Invalid YES/NO value: {v}")
            };
        }
    }

    private sealed record ProcResult(bool Success , bool TimedOut , int? ExitCode , string StdOut , string StdErr);
}

// ============================================================
// DTOs
// ============================================================
public sealed class PrfUploadResponseDto
{
    public Guid ProblemId { get; set; }
    public Guid TestsetId { get; set; }
    public string Note { get; set; } = "PRF192";
    public int TestcaseCount { get; set; }
    public string? TemplateFileName { get; set; }
    public string? StatementFileName { get; set; }
}

public sealed class PrfTestcaseListDto
{
    public Guid ProblemId { get; set; }
    public Guid TestsetId { get; set; }
    public string Note { get; set; } = "";
    public List<PrfTestcaseItemDto> Items { get; set; } = new();
}

public sealed class PrfTestcaseItemDto
{
    public Guid Id { get; set; }
    public int Ordinal { get; set; }
    public string FileName { get; set; } = "";
    public decimal Mark { get; set; }
    public bool RemoveSpaces { get; set; }
    public bool CaseSensitive { get; set; }
    public string? InputPreview { get; set; }
    public string? OutputPreview { get; set; }
}

public sealed class PrfSubmitRequestDto
{
    public Guid UserId { get; set; }
    public Guid ProblemId { get; set; }
    public Guid TestsetId { get; set; }
    public string Language { get; set; } = "c";
    public string SourceCode { get; set; } = "";
    public string? CompilerPath { get; set; } = "gcc";
    public int CompileTimeoutMs { get; set; } = 8000;
    public int RunTimeoutMs { get; set; } = 2000;
}

public sealed class PrfSubmitResponseDto
{
    public Guid SubmissionId { get; set; }
    public string Verdict { get; set; } = "";
    public decimal TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public PrfCompileDto Compile { get; set; } = new();
    public List<PrfCaseResultDto> Cases { get; set; } = new();
}

public sealed class PrfCompileDto
{
    public bool Success { get; set; }
    public int? ExitCode { get; set; }
    public string? StdOut { get; set; }
    public string? StdErr { get; set; }
}

public sealed class PrfCaseResultDto
{
    public int Ordinal { get; set; }
    public string Verdict { get; set; } = "";
    public decimal Score { get; set; }
    public decimal Mark { get; set; }
    public string? ExpectedPreview { get; set; }
    public string? ActualPreview { get; set; }
}