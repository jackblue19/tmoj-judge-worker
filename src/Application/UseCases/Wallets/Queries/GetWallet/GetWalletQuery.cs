using Application.UseCases.Wallets.Dtos;
using MediatR;

namespace Application.UseCases.Wallets.Queries.GetWallet
{
    public class GetWalletQuery : IRequest<GetWalletResult>
    {
    }
}