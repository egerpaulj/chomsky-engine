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

namespace Crawler.WebDriver.Core
{
    public interface IWebDriverMetrics
    {
        void IncDownload(string key);
        void IncPageLoad(string key);
    }

    public class WebDriverMetrics : IWebDriverMetrics
    {
        
        private readonly Counter _counter;

        public WebDriverMetrics()
        {
            _counter = Metrics.CreateCounter("crawler_web_driver", "Counts web driver requests", "Context");
            
        }
        public void IncDownload(string key)
        {
            _counter.WithLabels("download_request").Inc();
        }

        public void IncPageLoad(string key)
        {
            _counter.WithLabels("webpage_request").Inc();
        }
    }
}