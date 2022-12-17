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
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;

namespace Crawler.Core.UnitTest
{
    public class TestCaseFactoryTable
    {
        public static TestCase<ExpectedTable> Create()
        {
            var xml = @"<html><header></header>
                            <div>
                                someOthertest1
                                <a href='https://somethingElse/firstLink'/>
                            </div>
                            <div>
                                <div> 
                                    ParentTextDifferentStyle
                                    <table>
                                        <th>
                                            Header 1
                                        </th>
                                        <th>
                                            Header 2
                                        </th>
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
                                    <table>
                                        <th>You don't want me sir</th>
                                        <tr>
                                            <td>
                                                Defo not me
                                            </td>
                                        </tr>
                                    </table>
                                </div>
                            </div>
                        </html>";


            var request = CreateRequestDocumentPartTable();

            return new TestCase<ExpectedTable>()
            {
                CrawlRequest = request,
                Xml = xml,
                ExpectedResult = CreateExpectedTable()
            };
        }

        public static ExpectedTable CreateExpectedTable()
        {
            return new ExpectedTable
                {
                    Headers = new List<string>
                    {
                        "Header 1\n",
                        "Header 2\n"
                    },
                    Rows = new List<ExpectedRow>
                    {
                        new ExpectedRow
                        {
                            Content = new List<string>
                            {
                                "It could just be me",
                                "row data 2"
                            },
                        },
                        new ExpectedRow
                        {
                            Content = new List<string>
                            {
                                "row 2: 1",
                                "row 2: 2"
                            },
                        },
                        new ExpectedRow
                        {
                            Content = new List<string>
                            {
                                "row 3: 1",
                                "row 3: 2"
                            },
                        },
                        new ExpectedRow
                        {
                            Content = new List<string>
                            {
                                "row 4: 1",
                                "row 4: 2"
                            },
                        },
                    }
                };
        }

        public static CrawlRequest CreateRequestDocumentPartTable()
        {
            var request = new CrawlRequest();
            request.LoadPageRequest = new LoadPageRequest{Uri = "some uri"};
            request.RequestDocument = new Document()
            {
                RequestDocumentPart = new DocumentPartTable()
                {
                    BaseUri = "some uri",
                    Selector = new DocumentPartSelector()
                    {
                        Xpath= "//table",
                        ContentSpecificMatch = "row data 2"
                    },
                },

            };
            return request;
        }
    }

    public class ExpectedTable
    {
        public List<string> Headers = new List<string>();
        public List<ExpectedRow> Rows = new List<ExpectedRow>(); 

    }

    public class ExpectedRow
    {
        public List<string> Content = new List<string>();
    }
}