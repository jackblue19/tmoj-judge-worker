using Application.UseCases.Store.Commands.BuyFptItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers.v1.Store;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/store")]
[ApiController]
[Authorize] // Bắt buộc đăng nhập mới mua được đồ
public class StoreController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoreController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// API Mua vật phẩm từ cửa hàng
    /// </summary>
    /// <param name="command">Chứa ItemId cần mua</param>
    /// <returns>ID của bản ghi trong kho đồ mới tạo</returns>
    [HttpPost("buy")]
    public async Task<IActionResult> BuyItem([FromBody] BuyFptItemCommand command)
    {
        try
        {
            var inventoryId = await _mediator.Send(command);
            return Ok(new 
            { 
                inventoryId, 
                message = "Chúc mừng! Bạn đã mua vật phẩm thành công. Vui lòng kiểm tra kho đồ." 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách vật phẩm đang bán trong cửa hàng
    /// </summary>
    [HttpGet("items")]
    public async Task<IActionResult> GetItems()
    {
        var items = await _mediator.Send(new Application.UseCases.Store.Queries.GetFptItems.GetFptItemsQuery());
        return Ok(items);
    }

    /// <summary>
    /// Lấy kho đồ cá nhân của người dùng hiện tại
    /// </summary>
    [HttpGet("my-inventory")]
    public async Task<IActionResult> GetMyInventory()
    {
        try
        {
            var inventory = await _mediator.Send(new Application.UseCases.Store.Queries.GetMyInventory.GetMyInventoryQuery());
            return Ok(inventory);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
