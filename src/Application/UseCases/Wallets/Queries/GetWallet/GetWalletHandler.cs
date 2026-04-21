using Application.Common.Interfaces;
using Application.UseCases.Wallets.Dtos;
using MediatR;

namespace Application.UseCases.Wallets.Queries.GetWallet
{
    public class GetWalletHandler : IRequestHandler<GetWalletQuery, GetWalletResult>
    {
        private readonly IWalletRepository _walletRepo;
        private readonly ICurrentUserService _currentUser;

        public GetWalletHandler(
            IWalletRepository walletRepo,
            ICurrentUserService currentUser)
        {
            _walletRepo = walletRepo;
            _currentUser = currentUser;
        }

        public async Task<GetWalletResult> Handle(
            GetWalletQuery request,
            CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
                throw new Exception("Unauthorized");

            var userId = _currentUser.UserId.Value;

            var wallet = await _walletRepo.GetByUserIdAsync(userId);

            if (wallet == null)
            {
                wallet = new Domain.Entities.Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userId,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _walletRepo.CreateAsync(wallet);
                await _walletRepo.SaveChangesAsync();
            }

            return new GetWalletResult
            {
                Balance = wallet.Balance
            };
        }
    }
}