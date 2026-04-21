namespace Application.UseCases.Wallets.Queries.GetWalletTransactions
{
    public class WalletTransactionDto
    {
        public string Type { get; set; } = default!;
        public string Direction { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}