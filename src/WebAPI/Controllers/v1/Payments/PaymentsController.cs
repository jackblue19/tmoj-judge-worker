using Application.UseCases.Payments.Commands.CreateVnPayPayment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("vnpay")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var command = new CreateVnPayPaymentCommand
        {
            Amount = request.Amount,
            UserId = userId,
            IpAddress = ipAddress
        };

        var result = await _mediator.Send(command);

        return Ok(result);
    }
}

public class CreatePaymentRequest
{
    public decimal Amount { get; set; }
}