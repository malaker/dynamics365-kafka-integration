using System;
using System.Collections.Generic;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public class CommitOffsetsRequest
    {
        public List<ConsumerOffset> Offsets { get; set; }
        
    }

    public class ConsumerOffset
    {
        public string Topic { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
    }
}
