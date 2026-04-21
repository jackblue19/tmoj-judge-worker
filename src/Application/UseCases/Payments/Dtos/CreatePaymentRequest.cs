using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Payments.Dtos;


public class CreatePaymentRequest
{
    public decimal Amount { get; set; }
}
