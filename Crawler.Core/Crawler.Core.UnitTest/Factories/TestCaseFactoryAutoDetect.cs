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

namespace Crawler.Core.UnitTest.Factories
{
    public class TestCaseFactoryAutoDetect
    {
        public static string GetBrokenHtml()
        {
            return System.IO.File.ReadAllText("Resources/broken.html");
        }

        public static TestCase<ExpectedArticle> CreateBrokenHtmlTestCase()
        {
            var xml = GetBrokenHtml();


            var request = DocumentPartTestHelper.CreateRequestDocumentAutoDetect(@"https://something");

            return new TestCase<ExpectedArticle>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = new ExpectedArticle
                {
                    Title = "The article header\n",
                    Content = "Some test as title Sub Title I am content of various things With differently styled Content including an image And some more content link to something link to somewhere else Header 1 Header 2 It could just be me row data 2 row 2: 1row 2: 2 row 3: 1row 3: 2 row 4: 1row 4: 2\n",
                }
            };
        }
        
        public static TestCase<ExpectedArticle> CreateArticleTestCase()
        {
            var xml = @"<html>
                            <header>
                                <title>The article header</title>
                            </header>
                            <body>
                                <div class='titleClass'>
                                    Some test as title
                                    <a href='/firstLink'/>
                                </div>
                                <div class='content'>
                                    <div> 
                                        Sub Title
                                        <div>
                                            I am content <p>of various things</p>
                                            <div>
                                                With differently styled Content including an image
                                                <img src='pathToImage'/>
                                                And some more content
                                                <a href='/contentLink'>link to something</a>
                                                <a href='https://Somewhereelse/something'> link to somewhere else</a>
                                            </div>
                                            <table>
                                                <th>Header 1</th>
                                                <th>Header 2</th>
                                                <tr>
                                                    <td>
                                                        <div>
                                                            <a href='http://somethingElse/linkToSomewhere'>
                                                                It could just <p>be</p> me
                                                            </a>
                                                        </div>
                                                    </td>
                                                    <td>
                                                        row data 2
                                                    </td>
                                                </tr>
                                                <tr><td>row 2: 1</td><td>row 2: 2</td></tr>
                                                <tr><td>row 3: 1</td><td>row 3: 2</td></tr>
                                                <tr><td>row 4: 1</td><td>row 4: 2</td></tr>
                                            </table>
                                        </div>
                                    </div>
                                </div>
                            </body>
                        </html>";


            var request = DocumentPartTestHelper.CreateRequestDocumentAutoDetect(@"https://something");

            return new TestCase<ExpectedArticle>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = new ExpectedArticle
                {
                    Title = "The article header\n",
                    Content = "Some test as title Sub Title I am content of various things With differently styled Content including an image And some more content link to something link to somewhere else Header 1 Header 2 It could just be me row data 2 row 2: 1row 2: 2 row 3: 1row 3: 2 row 4: 1row 4: 2\n",
                    Links = new List<string>{@"https://something/firstLink", @"https://something/pathToImage", @"https://something/contentLink", @"https://somewhereelse/something", @"http://somethingelse/linkToSomewhere"},
                    Table = TestCaseFactoryTable.CreateExpectedTable()
                }
            };
        }
    }
}