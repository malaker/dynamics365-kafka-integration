using System.Net.Http;
using System.Threading.Tasks;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public interface IHttpKafkaProducer
    {
        Task<HttpResponseMessage> Produce<T>(T value, string topic);

        Task<HttpResponseMessage> Produce(dynamic value, string topic);
    }
}
