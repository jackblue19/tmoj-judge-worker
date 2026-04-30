using Application.UseCases.Store.Commands.BuyFptItem;
using Application.UseCases.Store.Commands.CreateFptItem;
using Application.UseCases.Store.Commands.UpdateFptItem;
using Application.UseCases.Store.Commands.DeleteFptItem;
using Application.UseCases.Store.Commands.UpdateUserInventory;
using Application.UseCases.Store.Commands.DeleteUserInventory;
using Application.UseCases.Store.Queries.GetFptItems;
using Application.UseCases.Store.Queries.GetFptItemDetail;
using Application.UseCases.Store.Queries.GetMyInventory;
using Application.UseCases.Store.Queries.GetUserInventoryDetail;
using Application.UseCases.Store.Commands.AddToCart;
using Application.UseCases.Store.Commands.RemoveFromCart;
using Application.UseCases.Store.Commands.Checkout;
using Application.UseCases.Store.Queries.GetCartItems;
using Application.UseCases.Store.Queries.GetAdminOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using System.IO;
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
    /// API Admin: Đăng món đồ mới vào cửa hàng (Hỗ trợ Upload Ảnh)
    /// </summary>
    [HttpPost("items")]
    [Authorize(Roles = "admin,manager")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateItem(
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] string itemType,
        [FromForm] decimal priceCoin,
        [FromForm] string? imageUrl,
        [FromForm] int? durationDays,
        [FromForm] int stockQuantity,
        [FromForm] string? metaJson,
        IFormFile? file)
    {
        try
        {
            System.Text.Json.JsonElement? meta = null;
            if (!string.IsNullOrEmpty(metaJson))
                meta = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(metaJson);

            System.IO.Stream? stream = null;
            string? ext = null;
            if (file != null && file.Length > 0)
            {
                stream = file.OpenReadStream();
                ext = System.IO.Path.GetExtension(file.FileName);
            }

            var command = new CreateFptItemCommand(name, description, itemType, priceCoin, imageUrl, durationDays, stockQuantity, meta, stream, ext);
            var itemId = await _mediator.Send(command);
            return Ok(new { itemId, message = "Đã đăng món đồ mới thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API Admin: Cập nhật thông tin món đồ (Hỗ trợ Upload Ảnh)
    /// </summary>
    [HttpPut("items/{id}")]
    [Authorize(Roles = "admin,manager")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] string itemType,
        [FromForm] decimal priceCoin,
        [FromForm] string? imageUrl,
        [FromForm] int? durationDays,
        [FromForm] int stockQuantity,
        [FromForm] string? metaJson,
        [FromForm] bool isActive,
        IFormFile? file)
    {
        try
        {
            System.Text.Json.JsonElement? meta = null;
            if (!string.IsNullOrEmpty(metaJson))
                meta = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(metaJson);

            System.IO.Stream? stream = null;
            string? ext = null;
            if (file != null && file.Length > 0)
            {
                stream = file.OpenReadStream();
                ext = System.IO.Path.GetExtension(file.FileName);
            }

            var command = new UpdateFptItemCommand(id, name, description, itemType, priceCoin, imageUrl, durationDays, stockQuantity, meta, isActive, stream, ext);
            var result = await _mediator.Send(command);
            return result ? Ok("Cập nhật thành công") : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API Admin: Lấy danh sách các sản phẩm đã được mua
    /// </summary>
    [HttpGet("admin/orders")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAdminOrders([FromQuery] GetAdminOrdersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(new
        {
            data = result.Items,
            message = "Success",
            success = true,
            totalCount = result.Total,
            page = result.Page,
            pageSize = result.PageSize
        });
    }

    /// <summary>
    /// API Admin: Xóa món đồ khỏi shop
    /// </summary>
    [HttpDelete("items/{id}")]
    [Authorize(Roles = "admin,manager")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var result = await _mediator.Send(new DeleteFptItemCommand(id));
        return result ? Ok("Đã xóa món đồ") : NotFound();
    }

    /// <summary>
    /// Lấy chi tiết món đồ trong shop
    /// </summary>
    [HttpGet("items/{id}")]
    public async Task<IActionResult> GetItemDetail(Guid id)
    {
        var item = await _mediator.Send(new GetFptItemDetailQuery(id));
        return item != null ? Ok(item) : NotFound();
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
        var items = await _mediator.Send(new GetFptItemsQuery());
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
            var inventory = await _mediator.Send(new GetMyInventoryQuery());
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

    /// <summary>
    /// Xem chi tiết một món đồ trong kho cá nhân
    /// </summary>
    [HttpGet("my-inventory/{id}")]
    public async Task<IActionResult> GetInventoryDetail(Guid id)
    {
        var result = await _mediator.Send(new GetUserInventoryDetailQuery(id));
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Trang bị hoặc Tháo bỏ vật phẩm
    /// </summary>
    [HttpPatch("my-inventory/{id}/equip")]
    public async Task<IActionResult> EquipItem(Guid id, [FromBody] UpdateUserInventoryRequest request)
    {
        try
        {
            var success = await _mediator.Send(new UpdateUserInventoryCommand(id, request.IsEquipped));
            if (!success) return NotFound(new { message = "Vật phẩm không tồn tại trong kho." });
            return Ok(new { message = request.IsEquipped ? "Đã trang bị vật phẩm." : "Đã tháo vật phẩm." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public record UpdateUserInventoryRequest(bool IsEquipped);

    /// <summary>
    /// Xóa/Bỏ vật phẩm khỏi kho đồ
    /// </summary>
    [HttpDelete("my-inventory/{id}")]
    public async Task<IActionResult> DeleteInventory(Guid id)
    {
        var result = await _mediator.Send(new DeleteUserInventoryCommand(id));
        return result ? Ok("Đã bỏ vật phẩm") : NotFound();
    }

    // --- CART (GIỎ HÀNG) ENDPOINTS ---

    /// <summary>
    /// Thêm vật phẩm vào giỏ hàng
    /// </summary>
    [HttpPost("cart")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { result, message = "Đã thêm vào giỏ hàng!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xem danh sách giỏ hàng của tôi
    /// </summary>
    [HttpGet("cart")]
    public async Task<IActionResult> GetCart()
    {
        var cart = await _mediator.Send(new GetCartItemsQuery());
        return Ok(cart);
    }

    /// <summary>
    /// Xóa vật phẩm khỏi giỏ hàng
    /// </summary>
    [HttpDelete("cart/{id}")]
    public async Task<IActionResult> RemoveFromCart(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new RemoveFromCartCommand(id));
            return result ? Ok("Đã xóa khỏi giỏ hàng") : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Thanh toán toàn bộ giỏ hàng
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        try
        {
            var result = await _mediator.Send(new CheckoutCommand());
            return Ok(new { result, message = "Thanh toán giỏ hàng thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
