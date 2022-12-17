//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2022  Paul Eger                                                                                                                                                                     

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
using Prometheus;

namespace Microservice.Exchange
{
    /// <summary>
    /// Keep track of Messages processed within the exchange.
    /// </summary>
    public interface IExchangeMetrics
    {
         void IncInput(string label);
         void IncOutput(string label);
         void IncError(string label);
    }

    public class ExchangeMetrics : IExchangeMetrics
    {
        private readonly Counter _inputCounter = Prometheus.Metrics.CreateCounter("datainput", "count of data input", "context");
        private readonly Counter _outputCounter = Prometheus.Metrics.CreateCounter("datainput", "count of data input", "context");
        private readonly Counter _errorCounter = Prometheus.Metrics.CreateCounter("datainput", "count of data input", "context");
        
        public void IncError(string label)
        {
            _errorCounter.WithLabels(label).Inc();
        }

        public void IncInput(string label)
        {
            _inputCounter.WithLabels(label).Inc();
        }

        public void IncOutput(string label)
        {
            _outputCounter.WithLabels(label).Inc();
        }
    }
}