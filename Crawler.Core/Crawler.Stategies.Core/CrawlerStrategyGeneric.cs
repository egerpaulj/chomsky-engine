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
using Crawler.Core.Metrics;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.WebDriver.Core;
using LanguageExt;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crawler.Core.Strategy
{
    public class CrawlerStrategyGeneric : CrawlerStrategyBase
    {
        private readonly IMetricRegister _metricRegister;

        public CrawlerStrategyGeneric(IWebDriverService driver, IMetricRegister metricRegister) : base(driver)
        {
            _metricRegister = metricRegister;
        }

        protected override TryOptionAsync<Unit> ProcessAnamolies(Option<Document> document, Option<string> uri)
        {
            return async () =>
            {
                var anomalies = document.Bind(doc => doc.RequestDocumentPart.Bind(d => d.GetAnomalies()));
                if (anomalies.Any())
                {
                    foreach (var anomaly in anomalies)
                    {
                        _metricRegister.IncrementAnomalyCount(anomaly);
                    }
                }

                return await Task.FromResult(Unit.Default);
            };
        }
    }
}