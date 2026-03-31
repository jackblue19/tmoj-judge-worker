using Contracts.Submissions.Judging;
using Microsoft.Extensions.Logging;

namespace Worker.Execution.Testset;

public sealed class TestsetLayoutAdapter
{
    private readonly TestsetPathResolver _resolver;
    private readonly ILogger<TestsetLayoutAdapter> _logger;

    public TestsetLayoutAdapter(
        TestsetPathResolver resolver ,
        ILogger<TestsetLayoutAdapter> logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    public async Task<PreparedJudgeCaseLayout> PrepareCaseAsync(
        string slug ,
        Guid testsetId ,
        DispatchJudgeCaseContract @case ,
        string workRoot ,
        CancellationToken ct)
    {
        var sourceInput = _resolver.GetCanonicalInputPath(slug , testsetId , @case.Ordinal);
        var sourceExpected = _resolver.GetCanonicalOutputPath(slug , testsetId , @case.Ordinal);

        if ( !File.Exists(sourceInput) )
            throw new FileNotFoundException($"Input file not found: {sourceInput}");

        if ( !File.Exists(sourceExpected) )
            throw new FileNotFoundException($"Expected output file not found: {sourceExpected}");

        var caseDir = Path.Combine(workRoot , @case.Ordinal.ToString("000"));
        Directory.CreateDirectory(caseDir);

        var mappedInput = Path.Combine(caseDir , "input.txt");
        var mappedExpected = Path.Combine(caseDir , "expected.txt");

        // batch đầu cứ copy cho chắc; sau này có thể tối ưu sang hardlink/symlink
        await CopyFileAsync(sourceInput , mappedInput , ct);
        await CopyFileAsync(sourceExpected , mappedExpected , ct);

        _logger.LogDebug(
            "Prepared case layout. Ordinal={Ordinal}, Input={Input}, Expected={Expected}" ,
            @case.Ordinal , mappedInput , mappedExpected);

        return new PreparedJudgeCaseLayout
        {
            TestcaseId = @case.TestcaseId ,
            Ordinal = @case.Ordinal ,
            Weight = @case.Weight ,
            IsSample = @case.IsSample ,
            InputPath = mappedInput ,
            ExpectedPath = mappedExpected ,
            CaseDirectory = caseDir
        };
    }

    private static async Task CopyFileAsync(string source , string destination , CancellationToken ct)
    {
        await using var src = File.OpenRead(source);
        await using var dst = File.Create(destination);
        await src.CopyToAsync(dst , ct);
    }
}

public sealed class PreparedJudgeCaseLayout
{
    public Guid TestcaseId { get; init; }
    public int Ordinal { get; init; }
    public int Weight { get; init; }
    public bool IsSample { get; init; }
    public string InputPath { get; init; } = null!;
    public string ExpectedPath { get; init; } = null!;
    public string CaseDirectory { get; init; } = null!;
}