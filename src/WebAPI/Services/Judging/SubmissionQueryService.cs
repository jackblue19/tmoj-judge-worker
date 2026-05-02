using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models.Common;
using WebAPI.Models.Submissions;

namespace WebAPI.Services.Judging;

public sealed class SubmissionQueryService
{
    private readonly TmojDbContext _db;

    public SubmissionQueryService(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<SubmissionDetailDto?> GetDetailAsync(
        Guid submissionId ,
        CancellationToken ct)
    {
        var submissionEntity = await _db.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == submissionId , ct);

        if ( submissionEntity is null )
            return null;

        var problem = await _db.Problems
            .AsNoTracking()
            .Where(x => x.Id == submissionEntity.ProblemId)
            .Select(x => new
            {
                x.Id ,
                x.Slug ,
                x.Title ,
                x.ProblemMode ,
                x.VisibilityCode ,
                x.TimeLimitMs ,
                x.MemoryLimitKb
            })
            .FirstOrDefaultAsync(ct);

        var runtime = submissionEntity.RuntimeId is null
            ? null
            : await _db.Runtimes
                .AsNoTracking()
                .Where(x => x.Id == submissionEntity.RuntimeId)
                .Select(x => new
                {
                    x.Id ,
                    x.RuntimeName ,
                    x.RuntimeVersion ,
                    x.ProfileKey ,
                    x.SourceFileName ,
                    x.CompileCommand ,
                    x.RunCommand
                })
                .FirstOrDefaultAsync(ct);

        var submission = new SubmissionDetailDto
        {
            SubmissionId = submissionEntity.Id ,
            UserId = submissionEntity.UserId ,

            ProblemId = submissionEntity.ProblemId ,
            ProblemSlug = problem?.Slug ,
            ProblemTitle = problem?.Title ,
            ProblemMode = problem?.ProblemMode ,
            ProblemVisibilityCode = problem?.VisibilityCode ,
            ProblemTimeLimitMs = problem?.TimeLimitMs ,
            ProblemMemoryLimitKb = problem?.MemoryLimitKb ,

            RuntimeId = submissionEntity.RuntimeId ,
            RuntimeName = runtime?.RuntimeName ,
            RuntimeVersion = runtime?.RuntimeVersion ,
            RuntimeProfileKey = runtime?.ProfileKey ,
            RuntimeSourceFileName = runtime?.SourceFileName ,
            RuntimeCompileCommand = runtime?.CompileCommand ,
            RuntimeRunCommand = runtime?.RunCommand ,

            TestsetId = submissionEntity.TestsetId ,

            StatusCode = submissionEntity.StatusCode ,
            VerdictCode = submissionEntity.VerdictCode ,

            FinalScore = submissionEntity.FinalScore ,
            TimeMs = submissionEntity.TimeMs ,
            MemoryKb = submissionEntity.MemoryKb ,

            SourceCode = submissionEntity.SourceCode ,
            Note = submissionEntity.Note ,

            CreatedAt = submissionEntity.CreatedAt ,
            JudgedAt = submissionEntity.JudgedAt
        };

        submission.LatestRun = await _db.JudgeRuns
            .AsNoTracking()
            .Where(x => x.SubmissionId == submissionId)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => new SubmissionRunDto
            {
                JudgeRunId = x.Id ,
                WorkerId = x.WorkerId ,
                Status = x.Status ,
                DockerImage = x.DockerImage ,
                Limits = x.Limits ,
                Note = x.Note ,
                CompileExitCode = x.CompileExitCode ,
                CompileTimeMs = x.CompileTimeMs ,
                TotalTimeMs = x.TotalTimeMs ,
                TotalMemoryKb = x.TotalMemoryKb ,
                StartedAt = x.StartedAt ,
                FinishedAt = x.FinishedAt
            })
            .FirstOrDefaultAsync(ct);

        submission.Results = await _db.Results
            .AsNoTracking()
            .Where(x => x.SubmissionId == submissionId)
            .OrderBy(x => x.Testcase != null ? x.Testcase.Ordinal : int.MaxValue)
            .ThenBy(x => x.CreatedAt)
            .Select(x => new SubmissionCaseResultDto
            {
                ResultId = x.Id ,
                TestcaseId = x.TestcaseId ,
                Ordinal = x.Testcase != null ? x.Testcase.Ordinal : null ,

                StatusCode = x.StatusCode ,

                RuntimeMs = x.RuntimeMs ,
                MemoryKb = x.MemoryKb ,

                CheckerMessage = x.CheckerMessage ,

                ExitCode = x.ExitCode ,
                Signal = x.Signal ,

                Message = x.Message ,
                Note = x.Note ,

                ExpectedOutput = x.ExpectedOutput ,
                ActualOutput = x.ActualOutput ,

                Type = x.Type ,

                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        submission.Diagnostic = BuildDiagnostic(submission);

        return submission;
    }

    public async Task<PagedResponse<SubmissionListItemDto>> SearchAsync(
        SubmissionSearchRequest req ,
        CancellationToken ct)
    {
        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = req.PageSize <= 0 ? 20 : req.PageSize;

        if ( pageSize > 100 )
            pageSize = 100;

        var query = _db.Submissions
            .AsNoTracking()
            .AsQueryable();

        if ( req.UserId.HasValue )
            query = query.Where(x => x.UserId == req.UserId.Value);

        if ( req.ProblemId.HasValue )
            query = query.Where(x => x.ProblemId == req.ProblemId.Value);

        if ( req.RuntimeId.HasValue )
            query = query.Where(x => x.RuntimeId == req.RuntimeId.Value);

        if ( !string.IsNullOrWhiteSpace(req.StatusCode) )
        {
            var normalizedStatus = req.StatusCode.Trim().ToLowerInvariant();
            query = query.Where(x => x.StatusCode == normalizedStatus);
        }

        if ( !string.IsNullOrWhiteSpace(req.VerdictCode) )
        {
            var normalizedVerdict = ResultStatusMapper.NormalizeVerdict(req.VerdictCode);
            query = query.Where(x => x.VerdictCode == normalizedVerdict);
        }

        if ( req.CreatedFromUtc.HasValue )
            query = query.Where(x => x.CreatedAt >= req.CreatedFromUtc.Value);

        if ( req.CreatedToUtc.HasValue )
            query = query.Where(x => x.CreatedAt <= req.CreatedToUtc.Value);

        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SubmissionListItemDto
            {
                SubmissionId = x.Id ,
                UserId = x.UserId ,

                ProblemId = x.ProblemId ,
                RuntimeId = x.RuntimeId ,

                StatusCode = x.StatusCode ,
                VerdictCode = x.VerdictCode ,

                FinalScore = x.FinalScore ,

                TimeMs = x.TimeMs ,
                MemoryKb = x.MemoryKb ,

                RuntimeName = x.Runtime != null ? x.Runtime.RuntimeName : null ,
                RuntimeVersion = x.Runtime != null ? x.Runtime.RuntimeVersion : null ,

                CreatedAt = x.CreatedAt ,
                JudgedAt = x.JudgedAt
            })
            .ToListAsync(ct);

        return new PagedResponse<SubmissionListItemDto>
        {
            Page = page ,
            PageSize = pageSize ,
            TotalItems = totalItems ,
            TotalPages = (int) Math.Ceiling(totalItems / (double) pageSize) ,
            Items = items
        };
    }

    private static SubmissionDiagnosticDto BuildDiagnostic(SubmissionDetailDto submission)
    {
        var verdict = ResultStatusMapper.NormalizeVerdict(submission.VerdictCode);

        var firstFailed = submission.Results
            .FirstOrDefault(x => ResultStatusMapper.NormalizeVerdict(x.StatusCode) != "ac");

        var rawMessage =
            FirstNotEmpty(
                firstFailed?.Message ,
                firstFailed?.CheckerMessage ,
                firstFailed?.Note ,
                submission.LatestRun?.Note ,
                submission.Note);

        if ( verdict == "ce" )
        {
            var wrongLanguage = LooksLikeWrongLanguage(
                submission.SourceCode ,
                submission.RuntimeName ,
                submission.RuntimeProfileKey ,
                rawMessage);

            return new SubmissionDiagnosticDto
            {
                Level = "error" ,
                Code = wrongLanguage ? "possible_wrong_language" : "compile_error" ,
                Title = wrongLanguage
                    ? "Có thể bạn đã chọn sai ngôn ngữ/runtime"
                    : "Lỗi biên dịch hoặc lỗi cú pháp" ,
                Message = rawMessage ?? "Chương trình không biên dịch được." ,
                Hints = wrongLanguage
                    ? new[]
                    {
                        $"Runtime hiện tại: {submission.RuntimeName} {submission.RuntimeVersion}".Trim(),
                        $"Problem mode: {submission.ProblemMode}",
                        "Kiểm tra lại ngôn ngữ bạn chọn trước khi submit.",
                        "Với pro mode, code phải là full code đúng ngôn ngữ, có main/class entrypoint phù hợp."
                    }
                    : new[]
                    {
                        "Kiểm tra syntax, thiếu dấu ;, thiếu include/import, sai tên class hoặc sai hàm main.",
                        "Với Java, public class thường cần khớp với Main.java.",
                        "Với C/C++, kiểm tra compiler standard và thư viện đang dùng."
                    }
            };
        }

        if ( verdict == "re" )
        {
            return new SubmissionDiagnosticDto
            {
                Level = "error" ,
                Code = "runtime_error" ,
                Title = "Runtime Error" ,
                Message = rawMessage ?? "Chương trình compile được nhưng bị crash khi chạy." ,
                Hints = new[]
                {
                    "Kiểm tra truy cập mảng ngoài phạm vi, chia cho 0, null reference, stack overflow.",
                    "Kiểm tra format input có đúng với đề không.",
                    "Xem exitCode, stderr/message của testcase đầu tiên bị lỗi."
                }
            };
        }

        if ( verdict == "tle" )
        {
            return new SubmissionDiagnosticDto
            {
                Level = "warning" ,
                Code = "time_limit_exceeded" ,
                Title = "Time Limit Exceeded" ,
                Message = "Chương trình vượt giới hạn thời gian." ,
                Hints = new[]
                {
                    "Kiểm tra độ phức tạp thuật toán.",
                    "Kiểm tra vòng lặp vô hạn.",
                    "Tối ưu input/output nếu cần."
                }
            };
        }

        if ( verdict == "mle" )
        {
            return new SubmissionDiagnosticDto
            {
                Level = "warning" ,
                Code = "memory_limit_exceeded" ,
                Title = "Memory Limit Exceeded" ,
                Message = "Chương trình vượt giới hạn bộ nhớ." ,
                Hints = new[]
                {
                    "Kiểm tra mảng/vector/list quá lớn.",
                    "Tránh lưu dữ liệu không cần thiết.",
                    "Kiểm tra recursion hoặc graph/tree tạo quá nhiều node."
                }
            };
        }

        if ( verdict == "wa" )
        {
            return new SubmissionDiagnosticDto
            {
                Level = "info" ,
                Code = "wrong_answer" ,
                Title = "Wrong Answer" ,
                Message = rawMessage ?? "Output không khớp expected output." ,
                Hints = new[]
                {
                    "So sánh actual output với expected output ở testcase được phép hiển thị.",
                    "Kiểm tra edge cases, off-by-one, overflow.",
                    "Kiểm tra format output và compare mode."
                }
            };
        }

        if ( verdict == "ac" )
        {
            return new SubmissionDiagnosticDto
            {
                Level = "success" ,
                Code = "accepted" ,
                Title = "Accepted" ,
                Message = "Bài làm đã đúng." ,
                Hints = Array.Empty<string>()
            };
        }

        return new SubmissionDiagnosticDto
        {
            Level = "info" ,
            Code = "unknown" ,
            Title = "Chưa xác định" ,
            Message = rawMessage ?? "Chưa có đủ thông tin diagnostic." ,
            Hints = new[]
            {
                "Kiểm tra latestRun.note, result.message, checkerMessage, exitCode."
            }
        };
    }

    private static bool LooksLikeWrongLanguage(
        string? sourceCode ,
        string? runtimeName ,
        string? runtimeProfileKey ,
        string? message)
    {
        var code = sourceCode ?? string.Empty;
        var runtime = Normalize($"{runtimeName} {runtimeProfileKey}");
        var msg = Normalize(message);

        var runtimeCpp = runtime.Contains("cpp") || runtime.Contains("c++");
        var runtimeJava = runtime.Contains("java");
        var runtimePython = runtime.Contains("python");

        var codePython =
            code.Contains("def ") ||
            code.Contains("import ") ||
            code.Contains("print(") ||
            code.Contains("input()");

        var codeCpp =
            code.Contains("#include") ||
            code.Contains("using namespace std") ||
            code.Contains("int main(");

        var codeJava =
            code.Contains("public class") ||
            code.Contains("static void main") ||
            code.Contains("System.out");

        if ( runtimeCpp && codePython )
            return true;

        if ( runtimePython && (codeCpp || codeJava) )
            return true;

        if ( runtimeJava && (codeCpp || codePython) )
            return true;

        if ( msg.Contains("expected unqualified-id") && codePython )
            return true;

        if ( msg.Contains("stray") && runtimeCpp )
            return true;

        if ( msg.Contains("unknown type name") && codePython )
            return true;

        if ( msg.Contains("syntaxerror") && !runtimePython )
            return true;

        return false;
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
    }
}