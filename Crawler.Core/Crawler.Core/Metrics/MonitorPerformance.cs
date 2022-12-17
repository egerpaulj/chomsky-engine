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
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LanguageExt;

namespace Crawler.Core.Metrics
{
    public static class MonitorPerformance
    {
        public static bool ShouldMonitorPerformance = true;

     

        public static  void Monitor(Action action, string legend)
        {
            if (!ShouldMonitorPerformance)
            {
                action();
                return;
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                action();
            }
            finally
            {
                stopWatch.Stop();
                var color = System.Console.ForegroundColor;
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine($"{legend} took: {stopWatch.ElapsedMilliseconds}ms");
                System.Console.ForegroundColor = color;
            }

        }

        public static async Task<T> MonitorAsync<T>(Func<Task<T>> action, string legend)
        {
            if (!ShouldMonitorPerformance)
            {
                return await action();
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                return await action();
            }
            finally
            {
                stopWatch.Stop();
                var color = System.Console.ForegroundColor;
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine($"{legend} took: {stopWatch.ElapsedMilliseconds}ms");
                System.Console.ForegroundColor = color;
            }

        }
    }

}