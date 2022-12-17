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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microservice.Amqp.Configuration
{
    /// <summary>
    /// AMQP Configuration which may consist several AMQP integration contexts.
    ///
    /// This allows a single configuration file for all AMQP integrations used.
    /// </summary>
    public class AmqpConfiguration
    {
        public const string AmqpConfigurationRoot = "Amqp";
        private readonly IConfiguration _configuration;

        public List<AmqpContextConfiguration> AmqpContexts { get; }

        public AmqpConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
            AmqpContexts = LoadContexts();
        }

        private List<AmqpContextConfiguration> LoadContexts()
        {
            var contexts = _configuration
                                .GetSection(AmqpConfigurationRoot)
                                .GetSection("Contexts")
                                .GetChildren();


            var contextConfigurations = new List<AmqpContextConfiguration>();

            foreach (var context in contexts)
            {
                var contextSection = context;
                var contextName = context.Key;

                var contextConfig = new AmqpContextConfiguration
                {
                    Name = contextName,
                    Exchange = contextSection.GetValue<string>("Exchange"),
                    QueueName = contextSection.GetValue<string>("Queue"),
                    RoutingKey = contextSection.GetValue<string>("RoutingKey"),
                    RetryCount = contextSection.GetValue<int>("RetryCount"),
                };

                contextConfig.Validate();
                contextConfigurations.Add(contextConfig);
            }

            return contextConfigurations;

        }
    }

    /// <summary>
    /// Defines a particular application of AMQP. 
    ///
    /// Describes The AMQP system's representation/configuration
    /// </summary>    
    public class AmqpContextConfiguration
    {
        /// <summary>
        /// The context name of the application/context/integration.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The AMQP Exchange name to publish messages.
        /// </summary>
        public string Exchange { get; set; }
        
        /// <summary>
        ///  The AMQP Queue name to subscribe messages.
        /// </summary>
        public string QueueName { get; set; }
        
        /// <summary>
        /// The AMQP RoutingKey helps the Exchange determine where/how to publish the message.
        /// </summary>
        public string RoutingKey { get; set; }
        
        /// <summary>
        /// The number of times the message was retried
        /// </summary>
        public int RetryCount { get; set; }

        public void Validate()
        {
            if (
                string.IsNullOrEmpty(Name)
                || (string.IsNullOrEmpty(Exchange) && string.IsNullOrEmpty(QueueName))
                )
                throw new System.Exception("Empty Configuration Values are not allowed. Either the Exchange or QueueName is needed for AMQP");
        }

    }
}