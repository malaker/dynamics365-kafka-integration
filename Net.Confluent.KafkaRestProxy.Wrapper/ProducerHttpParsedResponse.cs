using System;
using System.Collections.Generic;

namespace Net.Confluent.KafkaRestProxy.Wrapper
{
    public class ProducerHttpParsedResponse
    {
        public List<ProducerHttpParsedOffsetResponse> Offsets { get; set; }

    }
}

public class ProducerHttpParsedOffsetResponse
{
    public int Partition { get; set; }

    public long Offset { get; set; }
}
