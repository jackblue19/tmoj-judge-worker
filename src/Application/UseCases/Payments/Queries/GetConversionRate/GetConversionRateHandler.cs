using MediatR;
using Microsoft.Extensions.Configuration;
using Application.UseCases.Payments.Dtos;

namespace Application.UseCases.Payments.Queries.GetConversionRate
{
    public class GetConversionRateHandler
        : IRequestHandler<GetConversionRateQuery, GetConversionRateResult>
    {
        private readonly IConfiguration _config;

        public GetConversionRateHandler(IConfiguration config)
        {
            _config = config;
        }

        public Task<GetConversionRateResult> Handle(
            GetConversionRateQuery request,
            CancellationToken cancellationToken)
        {
            decimal rate;

            if (!decimal.TryParse(_config["Payment:VndToCoinRate"], out rate))
                rate = 1000m; // default fallback

            return Task.FromResult(new GetConversionRateResult
            {
                Rate = rate
            });
        }
    }
}