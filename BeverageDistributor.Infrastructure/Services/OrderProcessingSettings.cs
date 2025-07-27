namespace BeverageDistributor.Infrastructure.Services
{
    public class OrderProcessingSettings
    {
        public int MinOrderQuantity { get; set; } = 1000;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
    }
}
