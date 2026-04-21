using MediatR;
using Application.UseCases.Wallets.Dtos;

namespace Application.UseCases.Wallets.Queries.GetWalletTransactions
{
    public class GetWalletTransactionsQuery : IRequest<List<WalletTransactionDto>>
    {
        public int Page { get; set; } = 1;
    }
}