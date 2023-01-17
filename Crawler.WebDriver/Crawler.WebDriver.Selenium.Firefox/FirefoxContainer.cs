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
using OpenQA.Selenium.Firefox;

namespace Crawler.WebDriver.Selenium.Firefox
{
    public class FirefoxContainer : IDisposable
    {
        private bool disposedValue;

        internal FirefoxDriver Driver { get; }
        internal DateTime CreatedTimeUtc { get; }
        internal string HostUri { get; }

        internal bool MarkedForRemoval{get; private set;}
        
        // ToDO Need to sync removal and running container 
        //internal bool IsActive { get; set; }

        public FirefoxContainer(FirefoxDriver driver, string hostUri)
        {
            Driver = driver;
            CreatedTimeUtc = DateTime.UtcNow;
            HostUri = hostUri;
        }

        public void Discard() { MarkedForRemoval = true;}

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
                Driver?.Close();
                Driver?.Quit();
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FirefoxContainer()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}