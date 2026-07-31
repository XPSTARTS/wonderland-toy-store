namespace WonderlandBackend.Models
{
    public class EmailTask
    {
        public string Type { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
    }
}