using System.Diagnostics.Metrics;
using BeverageDistributor.Application.Interfaces;

namespace BeverageDistributor.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IMetricsService for tracking application metrics
    /// </summary>
    public class MetricsService : IMetricsService
    {
        private readonly Counter<long> _ordersProcessed;
        private readonly Histogram<double> _orderProcessingTime;
        private readonly UpDownCounter<int> _activeOrders;

        public MetricsService(
            Counter<long> ordersProcessed,
            Histogram<double> orderProcessingTime,
            UpDownCounter<int> activeOrders)
        {
            _ordersProcessed = ordersProcessed ?? throw new ArgumentNullException(nameof(ordersProcessed));
            _orderProcessingTime = orderProcessingTime ?? throw new ArgumentNullException(nameof(orderProcessingTime));
            _activeOrders = activeOrders ?? throw new ArgumentNullException(nameof(activeOrders));
        }

        /// <inheritdoc />
        public void IncrementOrdersProcessed(int count = 1, params KeyValuePair<string, object?>[] tags)
        {
            _ordersProcessed.Add(count, tags);
        }

        /// <inheritdoc />
        public void RecordOrderProcessingTime(double value, params KeyValuePair<string, object?>[] tags)
        {
            _orderProcessingTime.Record(value, tags);
        }

        /// <inheritdoc />
        public void TrackActiveOrder(int delta, params KeyValuePair<string, object?>[] tags)
        {
            _activeOrders.Add(delta, tags);
        }
    }
}
