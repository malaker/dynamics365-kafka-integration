using System;
using System.Collections.Generic;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public class KafkaRestProxyResponse<K, V>
    {
        public string Topic { get; set; }

        public K Key { get; set; }

        public V Value { get; set; }

        public int Partition { get; set; }

        public long Offset { get; set; }

    }


    public class KafkaRestProxyBatchResponse<K, V>
    {
        public List<KafkaRestProxyResponse<K, V>> Messages { get; set; }
    }


}
