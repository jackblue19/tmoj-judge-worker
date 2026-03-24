using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace WebAPI.Controllers.v1.TempStorage;

[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    private readonly TmojDbContext _context;

    public StorageController(TmojDbContext context)
    {
        _context = context;
    }

    // 🔍 GET: api/storage
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.StorageFiles
            .Include(x => x.Owner)
            .Include(x => x.Editorials)
            .ToListAsync();

        return Ok(data);
    }

    // 🔍 GET: api/storage/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _context.StorageFiles
            .Include(x => x.Owner)
            .Include(x => x.Editorials)
            .FirstOrDefaultAsync(x => x.StorageId == id);

        if ( item == null ) return NotFound();

        return Ok(item);
    }

    // ➕ POST: api/storage
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStorageDto dto)
    {
        // check owner exist
        var user = await _context.Users.FindAsync(dto.OwnerId);
        if ( user == null )
            return BadRequest("Owner không tồn tại");

        var entity = new StorageFile
        {
            StorageId = Guid.NewGuid() ,
            OwnerId = dto.OwnerId ,
            FileType = dto.FileType ,
            FilePath = dto.FilePath ,
            FileSize = dto.FileSize ,
            HashChecksum = dto.HashChecksum ,
            IsPrivate = dto.IsPrivate ,
            CreatedAt = DateTime.UtcNow ,
            ExpiresAt = dto.ExpiresAt
        };

        _context.StorageFiles.Add(entity);
        await _context.SaveChangesAsync();
        var result = new StorageResponseDto
        {
            StorageId = entity.StorageId ,
            FileType = entity.FileType ,
            FilePath = entity.FilePath ,
            IsPrivate = entity.IsPrivate ,
            Owner = new OwnerDto
            {
                UserId = entity.Owner.UserId ,
                Username = entity.Owner.Username
            }
        };

        return Ok(result);
        //return Ok(entity);
    }

    // ✏️ PUT: api/storage/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id , [FromBody] UpdateStorageDto dto)
    {
        var entity = await _context.StorageFiles.FindAsync(id);
        if ( entity == null ) return NotFound();

        if ( dto.FileType != null ) NormalizeFileType(dto.FileType);
        if ( dto.FilePath != null ) entity.FilePath = dto.FilePath;
        if ( dto.FileSize.HasValue ) entity.FileSize = dto.FileSize;
        if ( dto.IsPrivate.HasValue ) entity.IsPrivate = dto.IsPrivate.Value;
        if ( dto.ExpiresAt.HasValue ) entity.ExpiresAt = dto.ExpiresAt;

        await _context.SaveChangesAsync();

        var result = new StorageResponseDto
        {
            StorageId = entity.StorageId ,
            FileType = entity.FileType ,
            FilePath = entity.FilePath ,
            IsPrivate = entity.IsPrivate ,
            Owner = new OwnerDto
            {
                UserId = entity.Owner.UserId ,
                Username = entity.Owner.Username
            }
        };

        return Ok(result);

        //return Ok(entity);
    }

    // ❌ DELETE: api/storage/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.StorageFiles
            .Include(x => x.Editorials)
            .FirstOrDefaultAsync(x => x.StorageId == id);

        if ( entity == null ) return NotFound();

        // ⚠️ nếu có editorial thì xử lý trước
        if ( entity.Editorials.Any() )
        {
            return BadRequest("Storage đang được sử dụng trong Editorial");
        }

        _context.StorageFiles.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok("Deleted");
    }

    private string NormalizeFileType(string fileType)
    {
        fileType = fileType.ToLower();

        if ( fileType.Contains("code") ) return "source_code";
        if ( fileType.Contains("editorial") ) return "editorial";
        if ( fileType.Contains("test") ) return "testcase";
        if ( fileType.Contains("output") ) return "output";
        if ( fileType.Contains("log") ) return "log";

        return "report"; // fallback
    }
}