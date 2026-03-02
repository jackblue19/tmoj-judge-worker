using System.Security.Cryptography;
using Asp.Versioning;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebAPI.Extensions;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers.v1.ProblemManagement;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/problems")]
public class ProblemUploadsController : ControllerBase
{
    private readonly TmojDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly LocalStorageOptions _storage;

    public ProblemUploadsController(TmojDbContext db , IWebHostEnvironment env , IOptions<LocalStorageOptions> storage)
    {
        _db = db;
        _env = env;
        _storage = storage.Value;
    }

    [HttpPost("{id:guid}/uploads/request")]
    public async Task<ActionResult<ProblemUploadRequestResponseDto>> RequestUpload(
        Guid id ,
        [FromBody] ProblemUploadRequestDto? dto ,
        CancellationToken ct)
    {
        var problem = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( problem is null ) return NotFound();

        if ( !problem.IsActive || problem.StatusCode == "archived" )
            return Conflict("Problem is archived/inactive.");

        if ( problem.StatusCode == "published" )
            return Conflict("Cannot upload assets for a published problem.");

        if ( dto?.RequestedBy is Guid userId )
        {
            var ok = await _db.Users.AsNoTracking().AnyAsync(x => x.UserId == userId , ct);
            if ( !ok ) return BadRequest("Invalid requestedBy (user does not exist).");
        }

        return Ok(new ProblemUploadRequestResponseDto
        {
            UploadSessionId = Guid.NewGuid().ToString("N") ,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        });
    }

    [HttpPost("{id:guid}/testsets")]
    public async Task<ActionResult<ProblemTestsetResponseDto>> CreateTestset(
        Guid id ,
        [FromBody] ProblemTestsetCreateDto dto ,
        CancellationToken ct)
    {
        var problem = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( problem is null ) return NotFound();

        if ( !problem.IsActive || problem.StatusCode == "archived" )
            return Conflict("Problem is archived/inactive.");

        if ( problem.StatusCode == "published" )
            return Conflict("Cannot modify testsets for a published problem.");

        if ( dto.CreatedBy is Guid userId )
        {
            var ok = await _db.Users.AsNoTracking().AnyAsync(x => x.UserId == userId , ct);
            if ( !ok ) return BadRequest("Invalid createdBy (user does not exist).");
        }

        var testset = new Testset
        {
            Id = Guid.NewGuid() ,
            ProblemId = id ,
            Type = dto.Type.Trim() ,
            IsActive = true ,
            Note = dto.Note ,
            CreatedAt = DateTime.UtcNow ,
            CreatedBy = dto.CreatedBy ,
            StorageBlobId = null ,
            ExpireAt = dto.ExpireAt
        };

        _db.Testsets.Add(testset);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetTestset) ,
            new { id , testsetId = testset.Id , version = "1.0" } ,
            ToDto(testset)
        );
    }

    [HttpGet("{id:guid}/testsets/{testsetId:guid}")]
    public async Task<ActionResult<ProblemTestsetResponseDto>> GetTestset(Guid id , Guid testsetId , CancellationToken ct)
    {
        var testset = await _db.Testsets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == testsetId && x.ProblemId == id , ct);

        if ( testset is null ) return NotFound();
        return Ok(ToDto(testset));
    }

    [HttpPost("{id:guid}/testsets/{testsetId:guid}/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> UploadTestsetZip(
        Guid id ,
        Guid testsetId ,
        IFormFile file ,
        CancellationToken ct)
    {
        if ( file is null || file.Length == 0 ) return BadRequest("File is required.");

        var problem = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( problem is null ) return NotFound();

        if ( !problem.IsActive || problem.StatusCode == "archived" )
            return Conflict("Problem is archived/inactive.");

        if ( problem.StatusCode == "published" )
            return Conflict("Cannot upload assets for a published problem.");

        var testset = await _db.Testsets.FirstOrDefaultAsync(x => x.Id == testsetId && x.ProblemId == id , ct);
        if ( testset is null ) return NotFound("Testset not found.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if ( ext != ".zip" ) return BadRequest("Only .zip is supported.");

        var (blob, physicalPath) = await SaveBlobAsync(id , "testsets" , testsetId , file , ct);

        testset.StorageBlobId = blob.Id;
        testset.ExpireAt = null;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/testcases")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadTestcasesZip(
        Guid id ,
        [FromForm] UploadTestcasesFormDto form ,
        CancellationToken ct)
    {
        if ( form.File is null || form.File.Length == 0 )
            return BadRequest("File is required.");

        var problem = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( problem is null ) return NotFound();

        if ( !problem.IsActive || problem.StatusCode == "archived" )
            return Conflict("Problem is archived/inactive.");

        if ( problem.StatusCode == "published" )
            return Conflict("Cannot upload assets for a published problem.");

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            return BadRequest("Problem.slug is required to store testcases in local folder.");

        var testset = await _db.Testsets.FirstOrDefaultAsync(x => x.Id == form.TestsetId && x.ProblemId == id , ct);
        if ( testset is null ) return NotFound("Testset not found.");

        var ext = Path.GetExtension(form.File.FileName).ToLowerInvariant();
        if ( ext != ".zip" ) return BadRequest("Only .zip is supported.");

        var root = _storage.ProblemsRoot;
        if ( string.IsNullOrWhiteSpace(root) ) return Problem("LocalStorage.ProblemsRoot is not configured.");

        var slugSafe = SanitizeFolderName(problem.Slug);
        var testsetFolder = Path.Combine(root , slugSafe , form.TestsetId.ToString());
        Directory.CreateDirectory(testsetFolder);

        if ( form.ReplaceExisting )
        {
            foreach ( var dir in Directory.EnumerateDirectories(testsetFolder) )
                Directory.Delete(dir , true);
        }

        var tempDir = Path.Combine(Path.GetTempPath() , "tmoj_uploads" , Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var zipPath = Path.Combine(tempDir , "upload.zip");
        await using ( var fs = System.IO.File.Create(zipPath) )
            await form.File.CopyToAsync(fs , ct);

        ZipFile.ExtractToDirectory(zipPath , tempDir);

        var pairs = CollectTestcasePairs(tempDir);
        if ( pairs.Count == 0 )
            return BadRequest("No valid testcase pairs found. Expected *.inp/*.out or *-inp.txt/*-out.txt");

        foreach ( var kv in pairs.OrderBy(x => x.Key) )
        {
            var ordinal = kv.Key;
            var p = kv.Value;

            var ordinalFolder = Path.Combine(testsetFolder , ordinal.ToString("D3"));
            Directory.CreateDirectory(ordinalFolder);

            var inputExt = Path.GetExtension(p.InputPath).ToLowerInvariant();
            var outputExt = Path.GetExtension(p.OutputPath).ToLowerInvariant();

            var inputTarget = Path.Combine(ordinalFolder , inputExt == ".inp" ? "input.inp" : "input.txt");
            var outputTarget = Path.Combine(ordinalFolder , outputExt == ".out" ? "output.out" : "output.txt");

            System.IO.File.Copy(p.InputPath , inputTarget , true);
            System.IO.File.Copy(p.OutputPath , outputTarget , true);
        }

        Directory.Delete(tempDir , true);

        return Ok(new
        {
            problemId = id ,
            slug = problem.Slug ,
            testsetId = form.TestsetId ,
            savedTo = testsetFolder ,
            total = pairs.Count
        });
    }

    private static ProblemTestsetResponseDto ToDto(Testset x)
    {
        return new ProblemTestsetResponseDto
        {
            Id = x.Id ,
            ProblemId = x.ProblemId ,
            Type = x.Type ,
            IsActive = x.IsActive ,
            Note = x.Note ,
            StorageBlobId = x.StorageBlobId ,
            ExpireAt = x.ExpireAt ,
            CreatedAt = x.CreatedAt
        };
    }

    private async Task<(ArtifactBlob blob, string physicalPath)> SaveBlobAsync(
        Guid problemId ,
        string category ,
        Guid relatedId ,
        IFormFile file ,
        CancellationToken ct)
    {
        var root = Path.Combine(_env.ContentRootPath , "Storage" , "Problems" , problemId.ToString() , category , relatedId.ToString());
        Directory.CreateDirectory(root);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(root , fileName);

        await using ( var fs = System.IO.File.Create(physicalPath) )
        {
            await file.CopyToAsync(fs , ct);
        }

        byte[] hashBytes;
        await using ( var hs = System.IO.File.OpenRead(physicalPath) )
        {
            hashBytes = await SHA256.HashDataAsync(hs , ct);
        }

        var sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var sizeBytes = new FileInfo(physicalPath).Length;
        var contentType = file.ContentType;
        var storageUri = $"local://problems/{problemId}/{category}/{relatedId}/{fileName}";

        var blob = await _db.ArtifactBlobs.FirstOrDefaultAsync(x => x.Sha256 == sha256 && x.SizeBytes == sizeBytes , ct);
        if ( blob is null )
        {
            blob = new ArtifactBlob
            {
                Id = Guid.NewGuid() ,
                Sha256 = sha256 ,
                SizeBytes = sizeBytes ,
                ContentType = contentType ,
                StorageUri = storageUri ,
                CreatedAt = DateTime.UtcNow
            };
            _db.ArtifactBlobs.Add(blob);
            await _db.SaveChangesAsync(ct);
        }

        return (blob, physicalPath);
    }

    //  GET /api/v1/problems/{id}/testsets/{testsetId}/testcases?includePreview=true&previewChars=200
    [HttpGet("{id:guid}/testsets/{testsetId:guid}/testcases")]
    public async Task<ActionResult<TestcaseListDto>> GetAllTestcasesInTestset(
    Guid id ,
    Guid testsetId ,
    [FromQuery] bool includePreview = false ,
    [FromQuery] int previewChars = 200 ,
    CancellationToken ct = default)
    {
        var problem = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id , ct);
        if ( problem is null ) return NotFound("Problem not found.");

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            return BadRequest("Problem.slug is required.");

        var testsetExists = await _db.Testsets.AsNoTracking()
            .AnyAsync(x => x.Id == testsetId && x.ProblemId == id , ct);

        if ( !testsetExists ) return NotFound("Testset not found.");

        var root = _storage.ProblemsRoot;
        if ( string.IsNullOrWhiteSpace(root) ) return Problem("LocalStorage.ProblemsRoot is not configured.");

        var slugSafe = SanitizeFolderName(problem.Slug);
        var testsetFolder = Path.Combine(root , slugSafe , testsetId.ToString());

        if ( !Directory.Exists(testsetFolder) )
            return NotFound("Testset folder not found on disk.");

        previewChars = Math.Clamp(previewChars , 0 , 5000);

        var items = new List<TestcaseItemDto>();

        foreach ( var dir in Directory.EnumerateDirectories(testsetFolder) )
        {
            var folderName = Path.GetFileName(dir); // "001"
            if ( !int.TryParse(folderName , out var ordinalRaw) )
                continue;

            var inputPath = FindFirstExisting(dir , "input.inp" , "input.txt");
            var outputPath = FindFirstExisting(dir , "output.out" , "output.txt");

            if ( inputPath is null || outputPath is null )
                continue;

            var inputInfo = new FileInfo(inputPath);
            var outputInfo = new FileInfo(outputPath);

            var dto = new TestcaseItemDto
            {
                Ordinal = ordinalRaw , // 1,2,3 (nếu folder "001" -> 1)
                FolderName = folderName ,
                InputFileName = Path.GetFileName(inputPath) ,
                OutputFileName = Path.GetFileName(outputPath) ,
                InputSizeBytes = inputInfo.Length ,
                OutputSizeBytes = outputInfo.Length
            };

            if ( includePreview && previewChars > 0 )
            {
                dto.InputPreview = await ReadPreviewAsync(inputPath , previewChars , ct);
                dto.OutputPreview = await ReadPreviewAsync(outputPath , previewChars , ct);
            }

            items.Add(dto);
        }

        items = items.OrderBy(x => x.Ordinal).ToList();

        return Ok(new TestcaseListDto
        {
            ProblemId = id ,
            TestsetId = testsetId ,
            Slug = problem.Slug! ,
            RootPath = testsetFolder ,
            Total = items.Count ,
            Items = items
        });
    }

    private static string? FindFirstExisting(string dir , params string[] candidates)
    {
        foreach ( var name in candidates )
        {
            var path = Path.Combine(dir , name);
            if ( System.IO.File.Exists(path) )
                return path;
        }
        return null;
    }

    private static async Task<string> ReadPreviewAsync(string filePath , int maxChars , CancellationToken ct)
    {
        using var fs = System.IO.File.OpenRead(filePath);
        using var sr = new StreamReader(fs);

        var buffer = new char[maxChars];
        var read = await sr.ReadBlockAsync(buffer , 0 , maxChars);
        return new string(buffer , 0 , read);
    }

    private sealed record Pair(string InputPath , string OutputPath);

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Trim();
    }

    private static readonly Regex InpRegex = new(@"(?i)(?<idx>\d+)\.(inp)$" , RegexOptions.Compiled);
    private static readonly Regex OutRegex = new(@"(?i)(?<idx>\d+)\.(out)$" , RegexOptions.Compiled);
    private static readonly Regex InpTxtRegex = new(@"(?i)(?<idx>\d+)-inp\.txt$" , RegexOptions.Compiled);
    private static readonly Regex OutTxtRegex = new(@"(?i)(?<idx>\d+)-out\.txt$" , RegexOptions.Compiled);

    private static Dictionary<int , Pair> CollectTestcasePairs(string extractedRoot)
    {
        var inputs = new Dictionary<int , string>();
        var outputs = new Dictionary<int , string>();

        foreach ( var path in Directory.EnumerateFiles(extractedRoot , "*.*" , SearchOption.AllDirectories) )
        {
            var file = Path.GetFileName(path);

            var m1 = InpRegex.Match(file);
            if ( m1.Success && int.TryParse(m1.Groups["idx"].Value , out var idx1) )
            {
                inputs[idx1] = path;
                continue;
            }

            var m2 = OutRegex.Match(file);
            if ( m2.Success && int.TryParse(m2.Groups["idx"].Value , out var idx2) )
            {
                outputs[idx2] = path;
                continue;
            }

            var m3 = InpTxtRegex.Match(file);
            if ( m3.Success && int.TryParse(m3.Groups["idx"].Value , out var idx3) )
            {
                inputs[idx3] = path;
                continue;
            }

            var m4 = OutTxtRegex.Match(file);
            if ( m4.Success && int.TryParse(m4.Groups["idx"].Value , out var idx4) )
            {
                outputs[idx4] = path;
                continue;
            }
        }

        var result = new Dictionary<int , Pair>();
        foreach ( var idx in inputs.Keys.Intersect(outputs.Keys) )
            result[idx] = new Pair(inputs[idx] , outputs[idx]);

        return result;
    }

    //  testset - testcase
    [HttpGet("{id:guid}/testsets")]
    public async Task<ActionResult<TestsetListDto>> GetAllTestsets(
    Guid id ,
    CancellationToken ct)
    {
        var exists = await _db.Problems.AsNoTracking().AnyAsync(x => x.Id == id , ct);
        if ( !exists ) return NotFound("Problem not found.");

        var items = await _db.Testsets.AsNoTracking()
            .Where(x => x.ProblemId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TestsetDto
            {
                Id = x.Id ,
                ProblemId = x.ProblemId ,
                Type = x.Type ,
                IsActive = x.IsActive ,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new TestsetListDto
        {
            ProblemId = id ,
            Total = items.Count ,
            Items = items
        });
    }

    [HttpGet("{id:guid}/testsets/{testsetId:guid}/testcases/{ordinal:int}/input")]
    public async Task<IActionResult> GetTestcaseInput(
    Guid id ,
    Guid testsetId ,
    int ordinal ,
    CancellationToken ct)
    {
        var (ok, problem, testsetFolder, err) = await ResolveTestsetFolder(id , testsetId , ct);
        if ( !ok ) return err!;

        var folder = Path.Combine(testsetFolder , ordinal.ToString("D3"));
        if ( !Directory.Exists(folder) ) return NotFound("Testcase folder not found.");

        var inputPath = FindFirstExisting(folder , "input.inp" , "input.txt");
        if ( inputPath is null ) return NotFound("Input file not found.");

        return PhysicalFile(inputPath , "text/plain; charset=utf-8" , enableRangeProcessing: true);
    }

    [HttpGet("{id:guid}/testsets/{testsetId:guid}/testcases/{ordinal:int}/output")]
    public async Task<IActionResult> GetTestcaseOutput(
    Guid id ,
    Guid testsetId ,
    int ordinal ,
    CancellationToken ct)
    {
        var (ok, problem, testsetFolder, err) = await ResolveTestsetFolder(id , testsetId , ct);
        if ( !ok ) return err!;

        var folder = Path.Combine(testsetFolder , ordinal.ToString("D3"));
        if ( !Directory.Exists(folder) ) return NotFound("Testcase folder not found.");

        var outputPath = FindFirstExisting(folder , "output.out" , "output.txt");
        if ( outputPath is null ) return NotFound("Output file not found.");

        return PhysicalFile(outputPath , "text/plain; charset=utf-8" , enableRangeProcessing: true);
    }

    [HttpDelete("{id:guid}/testsets/{testsetId:guid}/testcases/{ordinal:int}")]
    public async Task<IActionResult> DeleteTestcase(
    Guid id ,
    Guid testsetId ,
    int ordinal ,
    CancellationToken ct)
    {
        var (ok, problem, testsetFolder, err) = await ResolveTestsetFolder(id , testsetId , ct);
        if ( !ok ) return err!;

        var folder = Path.Combine(testsetFolder , ordinal.ToString("D3"));
        if ( !Directory.Exists(folder) ) return NotFound("Testcase not found.");

        Directory.Delete(folder , true);
        return NoContent();
    }

    [HttpDelete("{id:guid}/testsets/{testsetId:guid}/testcases")]
    public async Task<IActionResult> DeleteTestcaseRange(
    Guid id ,
    Guid testsetId ,
    [FromBody] DeleteTestcaseRangeDto dto ,
    CancellationToken ct)
    {
        if ( dto.From > dto.To ) return BadRequest("From must be <= To.");

        var (ok, problem, testsetFolder, err) = await ResolveTestsetFolder(id , testsetId , ct);
        if ( !ok ) return err!;

        var deleted = 0;

        for ( var i = dto.From; i <= dto.To; i++ )
        {
            var folder = Path.Combine(testsetFolder , i.ToString("D3"));
            if ( !Directory.Exists(folder) ) continue;

            Directory.Delete(folder , true);
            deleted++;
        }

        return Ok(new { deleted });
    }

    [HttpPost("{id:guid}/testsets/{testsetId:guid}/testcases")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddSingleTestcase(
    Guid id ,
    Guid testsetId ,
    [FromForm] AddSingleTestcaseFormDto form ,
    CancellationToken ct)
    {
        var (ok, problem, testsetFolder, err) = await ResolveTestsetFolder(id , testsetId , ct);
        if ( !ok ) return err!;

        var folder = Path.Combine(testsetFolder , form.Ordinal.ToString("D3"));
        Directory.CreateDirectory(folder);

        if ( !form.Overwrite )
        {
            // nếu đã có testcase thì chặn
            var hasAny = FindFirstExisting(folder , "input.inp" , "input.txt") is not null
                         || FindFirstExisting(folder , "output.out" , "output.txt") is not null;

            if ( hasAny ) return Conflict("Testcase already exists. Set overwrite=true to replace.");
        }

        var inputTarget = BuildInputTarget(folder , form.Input.FileName);
        var outputTarget = BuildOutputTarget(folder , form.Output.FileName);

        await SaveFormFileAsync(form.Input , inputTarget , ct);
        await SaveFormFileAsync(form.Output , outputTarget , ct);

        return Ok(new { ordinal = form.Ordinal , folder = Path.GetFileName(folder) });
    }

    // helper testcase
    private static string BuildInputTarget(string folder , string originalName)
    {
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        return Path.Combine(folder , ext == ".inp" ? "input.inp" : "input.txt");
    }

    private static string BuildOutputTarget(string folder , string originalName)
    {
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        return Path.Combine(folder , ext == ".out" ? "output.out" : "output.txt");
    }

    private static async Task SaveFormFileAsync(IFormFile file , string path , CancellationToken ct)
    {
        await using var fs = System.IO.File.Create(path);
        await file.CopyToAsync(fs , ct);
    }

    //  advaced helper
    private async Task<(bool ok, Domain.Entities.Problem? problem, string testsetFolder, IActionResult? err)> ResolveTestsetFolder(
    Guid problemId ,
    Guid testsetId ,
    CancellationToken ct)
    {
        var problem = await _db.Problems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == problemId , ct);
        if ( problem is null ) return (false, null, "", NotFound("Problem not found."));

        if ( string.IsNullOrWhiteSpace(problem.Slug) )
            return (false, problem, "", BadRequest("Problem.slug is required."));

        var testsetExists = await _db.Testsets.AsNoTracking()
            .AnyAsync(x => x.Id == testsetId && x.ProblemId == problemId , ct);

        if ( !testsetExists ) return (false, problem, "", NotFound("Testset not found."));

        var root = _storage.ProblemsRoot;
        if ( string.IsNullOrWhiteSpace(root) )
            return (false, problem, "", Problem("LocalStorage.ProblemsRoot is not configured."));

        var slugSafe = SanitizeFolderName(problem.Slug);
        var folder = Path.Combine(root , slugSafe , testsetId.ToString());

        if ( !Directory.Exists(folder) )
            Directory.CreateDirectory(folder);

        return (true, problem, folder, null);
    }
}