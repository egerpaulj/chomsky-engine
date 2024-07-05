//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2024  Paul Eger                                                                                                                                                                     

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

namespace Microservice.Exchange.Core.Bertrand;

public interface IBertrandMetrics
{
    void IncIncoming(string key);
    void IncErrors(string key);
    void IncTransformed(string key);
    void IncPublished(string key);
}

public class BertrandMetrics : IBertrandMetrics
{
    private readonly Counter _incomingCounter = Prometheus.Metrics.CreateCounter("data_incoming", "count of incoming messages", "context");
    private readonly Counter _transformerCounter = Prometheus.Metrics.CreateCounter("data_transformed", "count of transormed messages", "context");
    private readonly Counter _publishedCounter = Prometheus.Metrics.CreateCounter("data_published", "count of published messages", "context");
    private readonly Counter _errorCounter = Prometheus.Metrics.CreateCounter("errors", "count of errors", "context");

    public void IncErrors(string key)
    {
        _errorCounter.WithLabels(key).Inc();
    }

    public void IncIncoming(string key)
    {
        _incomingCounter.WithLabels(key).Inc();
    }

    public void IncPublished(string key)
    {
        _publishedCounter.WithLabels(key).Inc();
    }

    public void IncTransformed(string key)
    {
        _transformerCounter.WithLabels(key).Inc();
    }
}