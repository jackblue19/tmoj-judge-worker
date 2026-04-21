using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Payments.Dtos
{
    public class VnPayReturnResult
    {
        public string RedirectUrl { get; set; } = default!;
    }
}
