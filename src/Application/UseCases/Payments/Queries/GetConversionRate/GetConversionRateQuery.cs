using MediatR;
using Application.UseCases.Payments.Dtos;

namespace Application.UseCases.Payments.Queries.GetConversionRate
{
    public class GetConversionRateQuery : IRequest<GetConversionRateResult>
    {
    }
}