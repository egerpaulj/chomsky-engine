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
using Crawler.WebDriver.Selenium.UserActions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Crawler.WebDriver.Selenium.Firefox.UserActions
{
    public class UserActionSelectDropdown : UserActionBase<string>
    {
        protected override void Execute(IWebElement element)
        {
            if (element.TagName.ToLower() == "input")
            {
                element.Click();
                element.SendKeys(Target.Match(t => t, () => throw new Exception("Dropdown selection without Target text is not possible for <input>")));
                return;
            }

            else if (element.TagName.ToLower() == "select")
            {
                var select = new SelectElement(element);

                Target.Match(t =>
                {

                    if (int.TryParse(t, out var index))
                        select.SelectByIndex(index);
                    else
                        select.SelectByText(t);

                }, () => select.SelectByIndex(0));

                return;
            }

            throw new Exception($"Unknown Dropdown element type: {element.TagName}. Text: {element.Text}");


        }
    }
}