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
using Crawler.Core.Parser.DocumentParts;
using LanguageExt;
using Prometheus;

namespace Crawler.Core.Metrics
{
    public interface IMetricRegister
    {
         void IncrementCrawlRequestCount();

         void IncrementCrawlCompletedCount();
         void IncrementCrawlFailedCount();

         void IncrementAnomalyCount(Option<Anomaly> anomaly);

    }

    public class MetricRegister : IMetricRegister
    {
        private readonly Counter _crawlCounter;

        public MetricRegister()
        {
            _crawlCounter = Prometheus.Metrics.CreateCounter("crawler", "crawler related counters", "context");
        }
        public void IncrementAnomalyCount(Option<Anomaly> anomaly)
        {
            anomaly.Match(a => {
                _crawlCounter.WithLabels($"anomaly_{a.AnomalyType.Match(at => at.ToString(), ()=> string.Empty)}").Inc();
            }, () => {});
            
        }

        public void IncrementCrawlCompletedCount()
        {
            _crawlCounter.WithLabels("completed").Inc();
        }

        public void IncrementCrawlFailedCount()
        {
            _crawlCounter.WithLabels("failed").Inc();
        }

        public void IncrementCrawlRequestCount()
        {
            _crawlCounter.WithLabels("request").Inc();
        }
    }
}