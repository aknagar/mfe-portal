namespace AugmentService.Core.Entities
{
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected,
        TimedOut
    }

    public class ApprovalRequest
    {
        public required string OrderId { get; set; }
        public required string OrderName { get; set; }
        public double TotalCost { get; set; }
        public int Quantity { get; set; }
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedBy { get; set; }
        public string? Comments { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
