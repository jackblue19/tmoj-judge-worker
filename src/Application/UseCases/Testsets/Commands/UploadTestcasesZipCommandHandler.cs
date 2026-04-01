using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using Application.UseCases.Testsets.Dtos;
using Ardalis.Specification;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace Application.UseCases.Testsets.Commands;

public sealed class UploadTestcasesZipCommandHandler
    : IRequestHandler<UploadTestcasesZipCommand , UploadTestcasesResultDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IReadRepository<Testset , Guid> _testsetReadRepository;
    private readonly IReadRepository<Testcase , Guid> _testcaseReadRepository;
    private readonly IWriteRepository<Testcase , Guid> _testcaseWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2Service _r2Service;
    private readonly int _maxParallelUploads;

    public UploadTestcasesZipCommandHandler(
        ICurrentUserService currentUser ,
        IReadRepository<Problem , Guid> problemReadRepository ,
        IReadRepository<Testset , Guid> testsetReadRepository ,
        IReadRepository<Testcase , Guid> testcaseReadRepository ,
        IWriteRepository<Testcase , Guid> testcaseWriteRepository ,
        IUnitOfWork unitOfWork ,
        IR2Service r2Service ,
        IConfiguration configuration)
    {
        _currentUser = currentUser;
        _problemReadRepository = problemReadRepository;
        _testsetReadRepository = testsetReadRepository;
        _testcaseReadRepository = testcaseReadRepository;
        _testcaseWriteRepository = testcaseWriteRepository;
        _unitOfWork = unitOfWork;
        _r2Service = r2Service;
        _maxParallelUploads = configuration.GetValue<int?>("TestsetSettings:MaxParallelUploads") ?? 4;
    }

    public async Task<UploadTestcasesResultDto> Handle(
        UploadTestcasesZipCommand request ,
        CancellationToken ct)
    {
        EnsureAuthenticated();

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if ( ext != ".zip" )
            throw new InvalidOperationException("Only .zip is supported.");

        var problem = await _problemReadRepository.GetByIdAsync(request.ProblemId , ct);
        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        EnsureCanManageProblem(problem);

        if ( !problem.IsActive || string.Equals(problem.StatusCode , "archived" , StringComparison.OrdinalIgnoreCase) )
            throw new InvalidOperationException("Problem is archived/inactive.");

        if ( string.Equals(problem.StatusCode , "published" , StringComparison.OrdinalIgnoreCase) )
            throw new InvalidOperationException("Cannot upload assets for a published problem.");

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            throw new InvalidOperationException("Problem.slug is required.");

        var testset = await _testsetReadRepository.GetByIdAsync(request.TestsetId , ct);
        if ( testset is null || testset.ProblemId != request.ProblemId )
            throw new KeyNotFoundException("Testset not found.");

        var tempRoot = Path.Combine(Path.GetTempPath() , "tmoj_uploads" , Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var zipPath = Path.Combine(tempRoot , "upload.zip");

            await using ( var fs = File.Create(zipPath) )
            {
                await request.FileStream.CopyToAsync(fs , ct);
            }

            var extractDir = Path.Combine(tempRoot , "extracted");
            Directory.CreateDirectory(extractDir);

            ZipFile.ExtractToDirectory(zipPath , extractDir);

            //var pairs = CollectTestcasePairsByFolder(extractDir);
            //if ( pairs.Count == 0 )
            //{
            //    throw new InvalidOperationException(
            //        "No valid testcase pairs found. Expected folder structure like 001/input.inp + output.out");
            //}

            var pairs = CollectTestcasePairsFlexible(extractDir);
            if ( pairs.Count == 0 )
            {
                throw new InvalidOperationException(
                    "No valid testcase pairs found. Supported formats: " +
                    "001/input.inp + output.out, " +
                    "flat test001.inp + test001.out, " +
                    "flat sub1_01.inp + sub1_01.out.");
            }

            var prefix = $"{request.TestsetId:D}/";

            if ( request.ReplaceExisting )
            {
                await _r2Service.DeleteByPrefixAsync("Testset" , prefix , ct);

                var existingTestcases = await _testcaseReadRepository.ListAsync(
                    new TestcasesByTestsetForWriteSpec(request.TestsetId) ,
                    ct);

                if ( existingTestcases.Count > 0 )
                {
                    _testcaseWriteRepository.RemoveRange(existingTestcases);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
            }

            var semaphore = new SemaphoreSlim(_maxParallelUploads);

            try
            {
                var uploadTasks = pairs
                    .OrderBy(x => x.Key)
                    .Select(async kv =>
                    {
                        await semaphore.WaitAsync(ct);
                        try
                        {
                            var ordinal = kv.Key;
                            var pair = kv.Value;

                            var inputExt = Path.GetExtension(pair.InputPath).ToLowerInvariant();
                            var outputExt = Path.GetExtension(pair.OutputPath).ToLowerInvariant();

                            var inputFileName = inputExt == ".inp" ? "input.inp" : "input.txt";
                            var outputFileName = outputExt == ".out" ? "output.out" : "output.txt";

                            var folder = $"{request.TestsetId:D}/{ordinal:D3}/";
                            var inputKey = folder + inputFileName;
                            var outputKey = folder + outputFileName;

                            await using var inputStream = File.OpenRead(pair.InputPath);
                            await using var outputStream = File.OpenRead(pair.OutputPath);

                            var inputUploadTask = _r2Service.UploadObjectAsync(
                                "Testset" ,
                                inputKey ,
                                inputStream ,
                                ResolveContentType(inputExt) ,
                                ct);

                            var outputUploadTask = _r2Service.UploadObjectAsync(
                                "Testset" ,
                                outputKey ,
                                outputStream ,
                                ResolveContentType(outputExt) ,
                                ct);

                            await Task.WhenAll(inputUploadTask , outputUploadTask);

                            return new UploadedPairResult
                            {
                                Ordinal = ordinal ,
                                InputObjectKey = inputKey ,
                                OutputObjectKey = outputKey
                            };
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                var uploadedResults = (await Task.WhenAll(uploadTasks))
                    .OrderBy(x => x.Ordinal)
                    .ToList();

                var newTestcases = uploadedResults
                    .Select(x => new Testcase
                    {
                        Id = Guid.NewGuid() ,
                        TestsetId = request.TestsetId ,
                        Ordinal = x.Ordinal ,
                        Weight = 1 ,
                        IsSample = x.Ordinal <= 3 ,
                        Input = x.InputObjectKey ,
                        ExpectedOutput = x.OutputObjectKey ,
                        StorageBlobId = null ,
                        ExpireAt = null
                    })
                    .ToList();

                if ( newTestcases.Count > 0 )
                {
                    await _testcaseWriteRepository.AddRangeAsync(newTestcases , ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                }

                return new UploadTestcasesResultDto
                {
                    ProblemId = request.ProblemId ,
                    Slug = problem.Slug ,
                    TestsetId = request.TestsetId ,
                    Total = uploadedResults.Count ,
                    Items = uploadedResults
                        .Select(x => new TestcaseUploadedItemDto
                        {
                            Ordinal = x.Ordinal ,
                            InputObjectKey = x.InputObjectKey ,
                            OutputObjectKey = x.OutputObjectKey
                        })
                        .ToList()
                };
            }
            finally
            {
                semaphore.Dispose();
            }
        }
        finally
        {
            if ( Directory.Exists(tempRoot) )
            {
                try
                {
                    Directory.Delete(tempRoot , recursive: true);
                }
                catch
                {
                }
            }
        }
    }

    private void EnsureAuthenticated()
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");
    }

    private void EnsureCanManageProblem(Problem problem)
    {
        var isAdmin = _currentUser.IsInRole("Admin");
        if ( isAdmin ) return;

        var currentUserId = _currentUser.UserId!.Value;

        if ( problem.CreatedBy != currentUserId )
            throw new KeyNotFoundException("Problem not found or access denied.");
    }

    private static string ResolveContentType(string extension)
    {
        return extension switch
        {
            ".inp" => "text/plain",
            ".out" => "text/plain",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    //  Parse zip file ver 1
    private static SortedDictionary<int , TestcasePair> CollectTestcasePairsByFolder(string root)
    {
        var result = new SortedDictionary<int , TestcasePair>();

        foreach ( var dir in Directory.EnumerateDirectories(root , "*" , SearchOption.AllDirectories) )
        {
            var dirName = Path.GetFileName(dir);

            if ( !int.TryParse(dirName , out var ordinal) )
                continue;

            var files = Directory.EnumerateFiles(dir).ToList();

            var inputPath =
                files.FirstOrDefault(x => string.Equals(Path.GetExtension(x) , ".inp" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => string.Equals(Path.GetFileName(x) , "input.txt" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => x.EndsWith("-inp.txt" , StringComparison.OrdinalIgnoreCase));

            var outputPath =
                files.FirstOrDefault(x => string.Equals(Path.GetExtension(x) , ".out" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => string.Equals(Path.GetFileName(x) , "output.txt" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => x.EndsWith("-out.txt" , StringComparison.OrdinalIgnoreCase));

            if ( inputPath is null || outputPath is null )
                continue;

            result[ordinal] = new TestcasePair
            {
                InputPath = inputPath ,
                OutputPath = outputPath
            };
        }

        return result;
    }

    //  parse zip file ver 2 with helpers
    private static SortedDictionary<int , TestcasePair> CollectTestcasePairsFlexible(string root)
    {
        // 1) Ưu tiên format thư mục số: 001/input.inp + output.out
        var byFolder = CollectTestcasePairsByNumericFolder(root);
        if ( byFolder.Count > 0 )
            return byFolder;

        // 2) Fallback: quét toàn bộ file phẳng / nested file
        var allFiles = Directory.EnumerateFiles(root , "*" , SearchOption.AllDirectories)
            .Where(f =>
            {
                var ext = Path.GetExtension(f);
                return ext.Equals(".inp" , StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".out" , StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileName(f).Equals("input.txt" , StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileName(f).Equals("output.txt" , StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith("-inp.txt" , StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith("-out.txt" , StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if ( allFiles.Count == 0 )
            return new SortedDictionary<int , TestcasePair>();

        // Group theo "logical testcase key"
        // ví dụ:
        // test001.inp / test001.out        -> test001
        // sub1_01.inp / sub1_01.out        -> sub1_01
        // foo-inp.txt / foo-out.txt        -> foo
        // input.txt / output.txt trong cùng folder -> folder relative path
        var grouped = new Dictionary<string , TempPair>(StringComparer.OrdinalIgnoreCase);

        foreach ( var file in allFiles )
        {
            var key = BuildLogicalPairKey(root , file);
            if ( key is null )
                continue;

            if ( !grouped.TryGetValue(key , out var pair) )
            {
                pair = new TempPair();
                grouped[key] = pair;
            }

            if ( IsInputFile(file) )
            {
                if ( pair.InputPath is not null )
                    throw new InvalidOperationException($"Duplicate input file detected for testcase key '{key}'.");
                pair.InputPath = file;
            }
            else if ( IsOutputFile(file) )
            {
                if ( pair.OutputPath is not null )
                    throw new InvalidOperationException($"Duplicate output file detected for testcase key '{key}'.");
                pair.OutputPath = file;
            }
        }

        var validPairs = grouped
            .Where(x => x.Value.InputPath is not null && x.Value.OutputPath is not null)
            .OrderBy(x => x.Key , StringComparer.OrdinalIgnoreCase)
            .ToList();

        if ( validPairs.Count == 0 )
            return new SortedDictionary<int , TestcasePair>();

        // Normalize ordinal liên tục 1..N
        var result = new SortedDictionary<int , TestcasePair>();
        var ordinal = 1;

        foreach ( var item in validPairs )
        {
            result[ordinal++] = new TestcasePair
            {
                InputPath = item.Value.InputPath! ,
                OutputPath = item.Value.OutputPath!
            };
        }

        return result;
    }

    private static SortedDictionary<int , TestcasePair> CollectTestcasePairsByNumericFolder(string root)
    {
        var result = new SortedDictionary<int , TestcasePair>();

        foreach ( var dir in Directory.EnumerateDirectories(root , "*" , SearchOption.AllDirectories) )
        {
            var dirName = Path.GetFileName(dir);

            if ( !int.TryParse(dirName , out var ordinal) )
                continue;

            var files = Directory.EnumerateFiles(dir).ToList();

            var inputPath =
                files.FirstOrDefault(IsInpExtension)
                ?? files.FirstOrDefault(x => Path.GetFileName(x).Equals("input.txt" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => x.EndsWith("-inp.txt" , StringComparison.OrdinalIgnoreCase));

            var outputPath =
                files.FirstOrDefault(IsOutExtension)
                ?? files.FirstOrDefault(x => Path.GetFileName(x).Equals("output.txt" , StringComparison.OrdinalIgnoreCase))
                ?? files.FirstOrDefault(x => x.EndsWith("-out.txt" , StringComparison.OrdinalIgnoreCase));

            if ( inputPath is null || outputPath is null )
                continue;

            result[ordinal] = new TestcasePair
            {
                InputPath = inputPath ,
                OutputPath = outputPath
            };
        }

        return result;
    }

    private static string? BuildLogicalPairKey(string root , string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var relativeDir = Path.GetRelativePath(root , Path.GetDirectoryName(filePath) ?? root);

        // input.txt / output.txt => key theo folder chứa nó
        if ( fileName.Equals("input.txt" , StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("output.txt" , StringComparison.OrdinalIgnoreCase) )
        {
            return $"folder::{relativeDir}";
        }

        // foo-inp.txt / foo-out.txt => key = foo
        if ( fileName.EndsWith("-inp.txt" , StringComparison.OrdinalIgnoreCase) )
            return $"name::{fileName[..^"-inp.txt".Length]}";

        if ( fileName.EndsWith("-out.txt" , StringComparison.OrdinalIgnoreCase) )
            return $"name::{fileName[..^"-out.txt".Length]}";

        // *.inp / *.out => key = file name without extension
        var ext = Path.GetExtension(fileName);
        if ( ext.Equals(".inp" , StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".out" , StringComparison.OrdinalIgnoreCase) )
        {
            return $"name::{Path.GetFileNameWithoutExtension(fileName)}";
        }

        return null;
    }

    private static bool IsInputFile(string path)
    {
        var fileName = Path.GetFileName(path);

        return IsInpExtension(path)
            || fileName.Equals("input.txt" , StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith("-inp.txt" , StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOutputFile(string path)
    {
        var fileName = Path.GetFileName(path);

        return IsOutExtension(path)
            || fileName.Equals("output.txt" , StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith("-out.txt" , StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInpExtension(string path)
        => Path.GetExtension(path).Equals(".inp" , StringComparison.OrdinalIgnoreCase);

    private static bool IsOutExtension(string path)
        => Path.GetExtension(path).Equals(".out" , StringComparison.OrdinalIgnoreCase);

    private sealed class TempPair
    {
        public string? InputPath { get; set; }
        public string? OutputPath { get; set; }
    }


    //  helper v1
    private sealed class TestcasePair
    {
        public string InputPath { get; init; } = null!;
        public string OutputPath { get; init; } = null!;
    }

    private sealed class UploadedPairResult
    {
        public int Ordinal { get; init; }
        public string InputObjectKey { get; init; } = null!;
        public string OutputObjectKey { get; init; } = null!;
    }

    private sealed class TestcasesByTestsetForWriteSpec : Specification<Testcase>
    {
        public TestcasesByTestsetForWriteSpec(Guid testsetId)
        {
            Query.Where(x => x.TestsetId == testsetId);
        }
    }
}