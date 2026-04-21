using Application.Common.Interfaces;
using Application.UseCases.Payments.Commands.CreateVnPayPayment;
using Application.UseCases.Payments.Commands.VnPayCallback;
using Application.UseCases.Payments.Dtos;
using Application.UseCases.Payments.Queries.GetConversionRate;
using Application.UseCases.Payments.Queries.VnPayReturn;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public PaymentsController(
        IMediator mediator,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
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
}