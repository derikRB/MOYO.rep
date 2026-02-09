namespace Sego_and__Bux.DTOs
{
    public class StockReceiptDto
    {
        // Links back to the purchase you previously recorded
        public int StockPurchaseId { get; set; }

        // Who received the stock (e.g. EmployeeName or ID)
        public string ReceivedBy { get; set; } = string.Empty;

        // Each line: how many units actually arrived
        public List<StockReceiptLineDto> Lines { get; set; } = new();
    }

    public class StockReceiptLineDto
    {
        public int ProductId { get; set; }
        public int QuantityReceived { get; set; }
    }
}
