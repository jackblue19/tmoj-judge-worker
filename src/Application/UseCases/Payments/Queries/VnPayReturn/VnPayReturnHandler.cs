using Application.Common.Interfaces;
using Application.UseCases.Payments.Dtos;
using MediatR;

namespace Application.UseCases.Payments.Queries.VnPayReturn
{
    public class VnPayReturnHandler
        : IRequestHandler<VnPayReturnQuery, VnPayReturnResult>
    {
        private readonly IVnPayService _vnPayService;
        private readonly IPaymentRepository _paymentRepo;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public VnPayReturnHandler(
            IVnPayService vnPayService,
            IPaymentRepository paymentRepo,
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _vnPayService = vnPayService;
            _paymentRepo = paymentRepo;
            _config = config;
        }

        public async Task<VnPayReturnResult> Handle(
            VnPayReturnQuery request,
            CancellationToken cancellationToken)
        {
            var query = request.Query;

            var frontendUrl = _config["Payment:FrontendReturnUrl"] 
                ?? "http://localhost:3000/payment-result";

            // ❗ Không throw, luôn redirect
            if (!_vnPayService.ValidateSignature(query))
            {
                return new VnPayReturnResult
                {
                    RedirectUrl = $"{frontendUrl}?status=invalid"
                };
            }

            var txnRef = query["vnp_TxnRef"].ToString();

            if (string.IsNullOrEmpty(txnRef))
            {
                return new VnPayReturnResult
                {
                    RedirectUrl = $"{frontendUrl}?status=missing"
                };
            }

            var payment = await _paymentRepo.GetByTxnRefAsync(txnRef);

            if (payment == null)
            {
                return new VnPayReturnResult
                {
                    RedirectUrl = $"{frontendUrl}?status=notfound"
                };
            }

            var status = payment.Status switch
            {
                "paid" => "success",
                "failed" => "failed",
                _ => "pending"
            };

            return new VnPayReturnResult
            {
                RedirectUrl =
                    $"{frontendUrl}?paymentId={payment.PaymentId}&status={status}"
            };
        }
    }
}