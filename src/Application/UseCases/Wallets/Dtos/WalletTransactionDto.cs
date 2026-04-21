using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Wallets.Dtos
{
    public class WalletTransactionDto
    {
        public string Type { get; set; } = default!; // deposit | withdraw
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}