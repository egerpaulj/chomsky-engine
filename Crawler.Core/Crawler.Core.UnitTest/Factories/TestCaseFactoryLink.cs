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
using System.Collections.Generic;
using Crawler.Core.Parser.DocumentParts;

namespace Crawler.Core.UnitTest
{
    public class TestCaseFactoryLink
    {
        public static TestCase<List<string>> CreateTestCaseAnchorOnly()
        {
            var xml = @"<html><header></header>
                            <div>
                                someOthertest1
                                <a href='/firstLink'/>
                            </div>
                            <div>
                                <div> 
                                    ParentTextDifferentStyle
                                    <div>
                                        <a href='linkToSomewhere'>
                                            It could just <p>be</p> me
                                        </a>
                                    </div>
                                </div>
                            </div>
                        </html>";


            var request = DocumentPartTestHelper.CreateRequestDocumentPartLinkText(@"https://something", "//a");

            return new TestCase<List<string>>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = new List<string>
                {
                        @"https://something/firstLink",
                        @"https://something/linkToSomewhere"
                }
            };
        }

        public static TestCase<List<string>> CreateTestCaseAnchorOnlyFullUri()
        {
            var xml = @"<html><header></header>
                            <div>
                                someOthertest1
                                <a href='https://somethingElse/firstLink'/>
                            </div>
                            <div>
                                <div> 
                                    ParentTextDifferentStyle
                                    <div>
                                        <a href='http://somethingElse/linkToSomewhere'>
                                            It could just <p>be</p> me
                                        </a>
                                    </div>
                                </div>
                            </div>
                        </html>";


            var request = DocumentPartTestHelper.CreateRequestDocumentPartLinkText(@"https://something", "//a");

            return new TestCase<List<string>>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = new List<string>
                {
                        @"https://somethingelse/firstLink",
                        @"http://somethingelse/linkToSomewhere"
                }
            };
        }

        public static TestCase<List<string>> CreateTestCaseAnchorAndContent()
        {
            var xml = @"<html><header></header>
                            <div>
                                someOthertest1
                                <a href='/firstLink'/>
                            </div>
                            <div>
                                <div> 
                                    ParentTextDifferentStyle
                                    <div>
                                        <a href='linkToSomewhere'>
                                            It could just <p>be</p> me
                                        </a>
                                    </div>
                                </div>
                            </div>
                        </html>";


            var request = DocumentPartTestHelper.CreateRequestDocumentPartLinkText(@"https://something", "//a", "It could just be me");

            return new TestCase<List<string>>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = new List<string>
                {
                        @"https://something/linkToSomewhere"
                }
            };
        }

    }
}