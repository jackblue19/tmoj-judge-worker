using Application.Common.Interfaces;
using Application.UseCases.Payments.Commands.CreatePayOsPayment;
using Application.UseCases.Payments.Commands.CreateVnPayPayment;
using Application.UseCases.Payments.Commands.HandlePayOsWebhook;
using Application.UseCases.Payments.Commands.VerifyPayOsPayment;
using Application.UseCases.Payments.Commands.VnPayCallback;
using Application.UseCases.Payments.Dtos;
using Application.UseCases.Payments.Queries.GetConversionRate;
using Application.UseCases.Payments.Queries.VnPayReturn;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _config;

    public PaymentsController(
        IMediator mediator,
        ICurrentUserService currentUser,
        IConfiguration config)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _config = config;
    }

    // =========================
    // CREATE PAYMENT
    // =========================
    [HttpPost("vnpay")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
        {
            return Unauthorized("User chưa đăng nhập");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var result = await _mediator.Send(new CreateVnPayPaymentCommand
        {
            Amount = request.Amount,
            UserId = _currentUser.UserId.Value,
            IpAddress = ipAddress
        });

        return Ok(result);
    }

    // =========================
    // CREATE PAYOS PAYMENT
    // =========================
    [HttpPost("payos")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> CreatePayOs([FromBody] CreatePaymentRequest request)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            return Unauthorized("User chưa đăng nhập");

        try
        {
            var result = await _mediator.Send(new CreatePayOsPaymentCommand
            {
                Amount = request.Amount,
                UserId = _currentUser.UserId.Value
            });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    // =========================
    // PAYOS VERIFY (FE gọi sau khi redirect về, backup cho webhook)
    // =========================
    [HttpPost("payos/verify")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> VerifyPayOs([FromBody] VerifyPayOsRequest request)
    {
        var result = await _mediator.Send(new VerifyPayOsPaymentCommand
        {
            OrderCode = request.OrderCode
        });
        return Ok(result);
    }

    // =========================
    // PAYOS WEBHOOK (SERVER TO SERVER)
    // =========================
    [HttpPost("payos/webhook")]
    public async Task<IActionResult> PayOsWebhook([FromBody] PayOsWebhookPayload payload)
    {
        var result = await _mediator.Send(new HandlePayOsWebhookCommand
        {
            Payload = payload
        });

        // PayOS yêu cầu trả về code "00" để xác nhận đã nhận webhook
        return Ok(new { code = "00", desc = result.Status });
    }

    // =========================
    // PAYOS RETURN (REDIRECT USER)
    // =========================
    [HttpGet("payos/return")]
    public IActionResult PayOsReturn(
        [FromQuery] string? code,
        [FromQuery] long? orderCode,
        [FromQuery] string? status)
    {
        var feUrl = (_config["urls-fe"] ?? throw new InvalidOperationException("Missing urls-fe config")).TrimEnd('/');
        var redirectUrl = $"{feUrl}/payment-result" +
                          $"?status={(status == "PAID" || code == "00" ? "success" : "cancel")}" +
                          $"&orderCode={orderCode}";
        return Redirect(redirectUrl);
    }

    // =========================
    // PAYOS CANCEL (REDIRECT USER)
    // =========================
    [HttpGet("payos/cancel")]
    public IActionResult PayOsCancel([FromQuery] long? orderCode)
    {
        var feUrl = (_config["urls-fe"] ?? throw new InvalidOperationException("Missing urls-fe config")).TrimEnd('/');
        var redirectUrl = $"{feUrl}/payment-cancel?orderCode={orderCode}";
        return Redirect(redirectUrl);
    }

    // =========================
    // VNPay CALLBACK (SERVER TO SERVER)
    // =========================
    [HttpGet("vnpay/callback")]
    public async Task<IActionResult> Callback()
    {
        var result = await _mediator.Send(new VnPayCallbackCommand
        {
            Query = Request.Query
        });

        if (result.Status == "invalid_signature")
            return BadRequest(result);

        if (result.Status == "missing_txn_ref")
            return BadRequest(result);

        if (result.Status == "not_found")
            return NotFound(result);

        return Ok(result);
    }

    // =========================
    // VNPay RETURN (REDIRECT USER)
    // =========================
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> Return()
    {
        var result = await _mediator.Send(new VnPayReturnQuery
        {
            Query = Request.Query
        });

        // 🔥 redirect về FE
        return Redirect(result.RedirectUrl);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetPaymentByIdQuery
        {
            PaymentId = id
        });

        return Ok(result);
    }

    [HttpGet("conversion-rate")]
    public async Task<IActionResult> GetRate()
    {
        var result = await _mediator.Send(new GetConversionRateQuery());
        return Ok(result);
    }

    // =========================
    // HISTORY
    // =========================
    [HttpGet("history/me")]
    public async Task<IActionResult> GetMyHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            return Unauthorized("User chưa đăng nhập");

        var result = await _mediator.Send(new Application.UseCases.Payments.Queries.GetMyPaymentHistory.GetMyPaymentHistoryQuery
        {
            UserId = _currentUser.UserId.Value,
            Page = page,
            PageSize = pageSize
        });

        return Ok(new
        {
            data = result.Items,
            pagination = new
            {
                totalItems = result.TotalItems,
                totalPages = result.TotalPages,
                page = result.Page,
                pageSize = result.PageSize
            },
            message = "Get my payment history successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("history/admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAdminHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new Application.UseCases.Payments.Queries.GetAllPaymentHistory.GetAllPaymentHistoryQuery
        {
            Page = page,
            PageSize = pageSize
        });

        return Ok(new
        {
            data = result.Items,
            pagination = new
            {
                totalItems = result.TotalItems,
                totalPages = result.TotalPages,
                page = result.Page,
                pageSize = result.PageSize
            },
            message = "Get all payment history successfully",
            traceId = HttpContext.TraceIdentifier
        });
    }
}