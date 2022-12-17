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
    public class TestCaseFactory
    {
        public static TestCase<string> CreateTestCaseTagBasedSimple()
        {
            var xml = @"<html>
                            <header>
                            </header>
                            <body>
                                <div>
                                    test1
                                </div>
                            </body>
                        </html>";

            var request = DocumentPartTestHelper.CreateRequestDocumentPartText("something", "//div");

            return new TestCase<string>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = "test1\n"
            };
        }

        public static TestCase<string> CreateTestCaseContentBasedTextComplex()
        {
            var xml = @"<html><header></header>
                        <body>
                            <div>
                                someOthertest1 I am some additional stuff
                            </div>
                            <div>
                                <div> 
                                    ParentTextDifferentStyle
                                    <div>
                                        test1 <p>I am some additional stuff</p>
                                    </div>
                                </div>
                            </div>
                        </body>
                        </html>";

            //var xml = @"<html><header></header><div>someOthertest1</div><div><div> ParentTextDifferentStyle<div> test1</div></div></div></html>";

            var request = DocumentPartTestHelper.CreateRequestDocumentPartText("something", "//*", "test1 I am some additional stuff");

            return new TestCase<string>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = "someOthertest1 I am some additional stuff ParentTextDifferentStyle test1 I am some additional stuff\n"
            };
        }

        public static TestCase<string> CreateTestCaseAttributeBasedComplex()
        {
            var xml = @"<html>
                            <header></header>
                            <div>
                                someOthertest1
                            </div>
                            <div>
                                <div class=""selectMe""> 
                                    ParentTextDifferentStyle
                                    <div> 
                                        test1
                                    </div>
                                </div>
                            </div>
                        </html>";

            var request = DocumentPartTestHelper.CreateRequestDocumentPartText("something", "//*[@class='selectMe']");

            return new TestCase<string>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = "ParentTextDifferentStyle test1\n"
            };
        }

        public static TestCase<string> CreateTestCaseAttributeAndContentBasedComplex()
        {
            var xml = @"<html>
                            <header></header>
                            <div>
                                someOthertest1
                            </div>
                            <div>
                                <div class=""random""> 
                                    ParentTextDifferentStyle
                                    <div class=""selectMe""> 
                                        I should be the only one (CCCCTestCCCCC)
                                    </div>
                                </div>
                            </div>
                            <div class=""selectMe""> 
                                I am not what you are looking for
                            </div>
                        </html>";

            var request = DocumentPartTestHelper.CreateRequestDocumentPartText("something", "//*[@class='selectMe' and contains(text(), 'should be')]");

            return new TestCase<string>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = "I should be the only one (CCCCTestCCCCC)\n"
            };
        }
    }
}