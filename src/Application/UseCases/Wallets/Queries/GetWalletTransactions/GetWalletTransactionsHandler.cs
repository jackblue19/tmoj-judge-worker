using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Wallets.Queries.GetWalletTransactions
{
    public class GetWalletTransactionsHandler
        : IRequestHandler<GetWalletTransactionsQuery, List<WalletTransactionDto>>
    {
        private readonly IWalletRepository _walletRepo;

        public GetWalletTransactionsHandler(IWalletRepository walletRepo)
        {
            _walletRepo = walletRepo;
        }

        public async Task<List<WalletTransactionDto>> Handle(
            GetWalletTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            var data = await _walletRepo.GetTransactionsAsync(
                request.UserId,
                request.Page,
                request.PageSize
            );

            return data.Select(x => new WalletTransactionDto
            {
                Type = x.Type,
                Direction = x.Direction,
                Amount = x.Amount,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            }).ToList();
        }
    }
}