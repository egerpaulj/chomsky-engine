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
using System.Threading.Tasks;
using LanguageExt;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace Crawler.WebDriver.Selenium.UserActions
{
    public class UserActionWait : UserAction
    {
        private const int timeoutInSeconds = 10;
        public Option<int> WaitInSeconds { get; set; }
        public override TryOptionAsync<Unit> Execute(FirefoxDriver driver)
        {
            return async () =>
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
                wait.PollingInterval = TimeSpan.FromMilliseconds(300);
                wait.Until(d => driver.ExecuteScript("return document.readyState").Equals("complete"));

                await  WaitInSeconds.MatchAsync(async waitSeconds => 
                {
                    driver.ExecuteScript("window.scrollBy(0, 500)");
                    await Task.Delay(waitSeconds);
                    driver.ExecuteScript("window.scrollBy(0, -500)");
                    return Task.CompletedTask;
                }, () => Task.CompletedTask);

                var element = XPath.Match(xpath =>
                        wait.Until(d =>
                            driver.FindElement(By.XPath(xpath))), () => {});

                return await Task.FromResult(Unit.Default);
            };
        }
    }
}