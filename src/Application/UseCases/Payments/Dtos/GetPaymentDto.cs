using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Payments.Dtos
{
    public class GetPaymentDto
    {
        public Guid PaymentId { get; set; }
        public string Status { get; set; } = default!;
        public decimal Amount { get; set; }
    }
}
