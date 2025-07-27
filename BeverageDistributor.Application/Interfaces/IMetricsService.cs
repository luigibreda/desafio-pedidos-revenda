using System.Diagnostics.Metrics;

namespace BeverageDistributor.Application.Interfaces
{
    /// <summary>
    /// Service for tracking application metrics
    /// </summary>
    public interface IMetricsService
    {
        /// <summary>
        /// Increments the orders processed counter
        /// </summary>
        /// <param name="count">Number of orders to add (default: 1)</param>
        /// <param name="tags">Optional tags for additional dimensions</param>
        void IncrementOrdersProcessed(int count = 1, params KeyValuePair<string, object?>[] tags);

        /// <summary>
        /// Records the time taken to process an order
        /// </summary>
        /// <param name="value">Time taken in seconds</param>
        /// <param name="tags">Optional tags for additional dimensions</param>
        void RecordOrderProcessingTime(double value, params KeyValuePair<string, object?>[] tags);

        /// <summary>
        /// Tracks the number of active orders
        /// </summary>
        /// <param name="delta">Change in the number of active orders</param>
        /// <param name="tags">Optional tags for additional dimensions</param>
        void TrackActiveOrder(int delta, params KeyValuePair<string, object?>[] tags);
    }
}
