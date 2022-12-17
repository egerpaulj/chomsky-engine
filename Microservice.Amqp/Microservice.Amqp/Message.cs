using System;
using LanguageExt;

namespace Microservice.Amqp
{
    /// <summary>
    /// The Message in the AMQP System.
    /// <para>
    /// Note: Additional properties are instroduced which are specific to Microservices used by this library. </para>
    /// </summary>
    public class Message<T>
    {
        /// <summary>
        /// The Actual Data.
        /// </summary>
        public Option<T> Payload { get; set; }

        /// <summary>
        /// A Unique Id of the message.
        /// </summary>
        public Option<Guid> Id { get; set; }

        /// <summary>
        /// The messages routing key.
        /// </summary>
        public Option<string> RoutingKey { get; set; }

        /// <summary>
        /// The CorrelationID of the Message that's specific to the requested operation(s).
        /// </summary>
        public Option<Guid> CorrelationId { get; set; }

        /// <summary>
        /// Keeps track of the  number of times the message was processed and failed.
        /// </summary>
        public Option<int> RetryCount { get; set; }

        /// <summary>
        /// The AMQP context. The context defines the integration context; explicit in the <see cref="Configuration.AmqpConfiguration"/>.
        /// </summary>
        public Option<string> Context { get; set; }
        public string MessageType => typeof(T).Name;

    }
}