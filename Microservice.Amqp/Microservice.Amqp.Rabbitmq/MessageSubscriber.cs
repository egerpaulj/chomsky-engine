//      Microservice AMQP Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2021  Paul Eger                                                                                                                                                                     

//      This program is free software: you can redistribute it and/or modify                                                                                                                                          
//      it under the terms of the GNU General Public License as published by                                                                                                                                          
//      the Free Software Foundation, either version 3 of the License, or                                                                                                                                             
//      (at your option) any later version.                                                                                                                                                                           

//      This program is distributed in the hope that it will be useful,                                                                                                                                               
//      but WITHOUT ANY WARRANTY; without even the implied warranty of                                                                                                                                                
//      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                                                                                                                                                 
//      GNU General Public License for more details.                                                                                                                                                                  

//      You should have received a copy of the GNU General Public License                                                                                                                                             
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Amqp.Rabbitmq.Configuration;
using Microservice.Serialization;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microservice.Amqp.Rabbitmq
{
    public class MessageSubscriber<T, R> : IMessageSubscriber<T, R>
    {
        private readonly RabbitMqSubscriberConfig _rabbitmqConfig;
        private readonly IJsonConverterProvider _jsonConverterProvider;
        private readonly IMessageHandler<T, R> _messageHandler;
        private IConnection _connection;
        private IModel _channel;

        private AsyncEventingBasicConsumer _consumer;
        private bool disposedValue;
        private readonly IObservable<Either<R, Exception>> _observable;
        private readonly IObservable<Either<Message<R>, Exception>> _fullMessageObservable;

        private readonly IConnectionFactory _connectionFactory;

        private event EventHandler<Either<Message<R>, Exception>> MessageReceived;
        

        public MessageSubscriber(
            RabbitMqSubscriberConfig rabbitmqConfig,
            IJsonConverterProvider jsonConverterProvider,
            IRabbitMqConnectionFactory connectionFactory,
            IMessageHandler<T, R> messageHandler)
        {
            _rabbitmqConfig = rabbitmqConfig;
            _jsonConverterProvider = jsonConverterProvider;
            _messageHandler = messageHandler;
            _connectionFactory = connectionFactory.CreateConnectionFactory(_rabbitmqConfig);

            _fullMessageObservable = Observable.FromEventPattern<Either<Message<R>, Exception>>(
                h => MessageReceived += h,
                h => MessageReceived -= h)
                .Select(m => m.EventArgs);

            _observable = _fullMessageObservable
                .Select(m => m.MapLeft(m => m.Payload.Match(p => p, () => throw new Exception("Empty message"))));
        }

        public IObservable<Either<R, Exception>> GetObservable()
        {
            if (disposedValue)
                throw new ObjectDisposedException("Message Subscriber has been disposed");

            return _observable;
        }

        public IObservable<Either<Message<R>, Exception>> GetMessageObservable()
        {
            if (disposedValue)
                throw new ObjectDisposedException("Message Subscriber has been disposed");

            return _fullMessageObservable;
        }

        public void Start()
        {
            if (disposedValue)
                throw new ObjectDisposedException("Message Subscriber has been disposed");

            // note: not thread safe
            if (_connection != null)
                return;

            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _consumer = new AsyncEventingBasicConsumer(_channel);

            _consumer.Received += OnAmqpMessageReceived;
            _channel.BasicConsume(queue: _rabbitmqConfig.QueueName, autoAck: false, consumer: _consumer);
        }

        private async Task<Either<Message<R>, Exception>> HandleMessage(MqMessageEvent<T> messageEvent)
        {
            try
            {
                var result = await _messageHandler.HandleMessage(messageEvent.Message.Payload);

                // ACK - message will be removed from queue
                _channel.BasicAck(messageEvent.DeliveryTag, false);
                return new Message<R>
                {
                    Payload = result,
                    Context = messageEvent.Message.Context,
                    CorrelationId = messageEvent.Message.CorrelationId,
                    Id = messageEvent.Message.Id
                };
            }
            catch (Exception e)
            {
                // NACK - message will be moved to the Deadletter exchange=> queue
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                _channel.BasicNack(messageEvent.DeliveryTag, false, false);
                return e;
            }
        }

        private MqMessageEvent<T> Map(BasicDeliverEventArgs ea)
        {
            try
            {
                var resultStr = Encoding.UTF8.GetString(ea.Body.ToArray());
                var result = _jsonConverterProvider.Deserialize<T>(resultStr);

                var id = ea.BasicProperties.Headers.ContainsKey("Id") 
                ? Guid.Parse(Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["Id"]))
                : Guid.NewGuid();

                var message = new Message<T>
                {
                    Payload = result,
                    CorrelationId = Guid.Parse(ea.BasicProperties.CorrelationId),
                    Id = id,
                    RetryCount = int.Parse(ea.BasicProperties.Headers["RetryCount"]?.ToString()),
                    Context = ea.BasicProperties.Headers.ContainsKey("Context") ? Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["Context"]) : string.Empty
                };


                return new MqMessageEvent<T>
                {
                    Message = message,
                    DeliveryTag = ea.DeliveryTag
                };
            }
            catch (Exception)
            {
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                throw;
            }
        }

        private async Task OnAmqpMessageReceived(object obj, BasicDeliverEventArgs args)
        {
            var message = Map(args);
            var result = await HandleMessage(message);

            MessageReceived?.Invoke(obj, result);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Console.WriteLine("Disposing - closing mq connections");
                    _channel.Close();
                    _channel.Dispose();
                    _connection.Close();
                    _connection.Dispose();

                    _consumer = null;
                    MessageReceived = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class MqMessageEvent<T>
    {
        public Message<T> Message { get; set; }
        public ulong DeliveryTag { get; set; }
    }
}