using Microsoft.AspNetCore.Mvc;
using Application.UseCases.Wallets.Queries.GetWallet;
using MediatR;

namespace WebAPI.Controllers.v1.Wallets

{
    [ApiController]
    [Route("api/v1/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WalletController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            var result = await _mediator.Send(new GetWalletQuery());
            return Ok(result);
        }
    }
}
