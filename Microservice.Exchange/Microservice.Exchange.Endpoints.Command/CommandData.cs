using System;
using System.Collections.Generic;
using LanguageExt;

namespace Microservice.Exchange.Endpoints.Command
{
    public class CommandData : IMessage
    {
        public string Command { get; set; }
        public string Arguments { get; set; }
        public string StdOut { get; set; }
        public string StdError { get; set; }
        public Option<Guid> Id { get; set; }
        public Option<Guid> CorrelationId { get; set; }
        public Option<string> RoutingKey { get; set; }
        public Option<List<KeyValuePair<string, string>>> Properties { get; set; }
    }
}