using System.Threading.Tasks;

namespace BeverageDistributor.Application.Interfaces
{
    public interface IMessageProducer
    {
        Task PublishOrderAsync<T>(T message, string queueName) where T : class;
    }
}
