using Microsoft.AspNetCore.Mvc;
using Application.UseCases.Wallets.Queries.GetWallet;
using Application.UseCases.Wallets.Queries.GetWalletTransactions;
using MediatR;
using Application.Common.Interfaces;

namespace WebAPI.Controllers.v1.Wallets
{
    [ApiController]
    [Route("api/v1/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public WalletController(
            IMediator mediator,
            ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        // =========================
        // GET WALLET
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return Unauthorized("User chưa đăng nhập");
            }

            var result = await _mediator.Send(new GetWalletQuery
            {
                UserId = _currentUser.UserId.Value
            });

            return Ok(result);
        }

        // =========================
        // GET WALLET TRANSACTIONS
        // =========================
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return Unauthorized("User chưa đăng nhập");
            }

            var result = await _mediator.Send(new GetWalletTransactionsQuery
            {
                UserId = _currentUser.UserId.Value,
                Page = page
            });

            return Ok(result);
        }
    }
}