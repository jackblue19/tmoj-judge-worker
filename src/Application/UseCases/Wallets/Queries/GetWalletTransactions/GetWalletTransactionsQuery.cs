using Application.UseCases.Wallets.Dtos;
using MediatR;

namespace Application.UseCases.Wallets.Queries.GetWalletTransactions
{
    public class GetWalletTransactionsQuery : IRequest<List<WalletTransactionDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}