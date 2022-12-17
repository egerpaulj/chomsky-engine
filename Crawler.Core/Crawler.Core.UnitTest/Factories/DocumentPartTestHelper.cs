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
using System.Collections.Generic;
using Crawler.Core.Parser;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Requests;
using HtmlAgilityPack;
using LanguageExt;

namespace Crawler.Core.UnitTest
{
    public class DocumentPartTestHelper
    {
        public static CrawlRequest CreateRequestDocumentPartText(string uri = null,string xpath="", string content = null)
        {
            var request = new CrawlRequest();
            request.CrawlId = Guid.NewGuid();
            request.LoadPageRequest = new LoadPageRequest{Uri = uri};
            request.RequestDocument = new Document()
            {
                DownloadContent = true,
                RequestDocumentPart = new DocumentPartText()
                {
                    Selector = new DocumentPartSelector()
                    {
                        
                        Xpath = xpath,
                        ContentSpecificMatch = content
                    },

                },

            };
            return request;
        }

        public static CrawlRequest CreateRequestDocumentPartLinkText(string uri = null, string xpath="", string content = null)
        {
            var request = new CrawlRequest();
            request.LoadPageRequest = new LoadPageRequest{Uri = uri};
            request.RequestDocument = new Document()
            {
                DownloadContent = true,
                RequestDocumentPart = new DocumentPartLink()
                {
                    BaseUri = uri,
                    Selector = new DocumentPartSelector()
                    {
                        Xpath = xpath,
                        ContentSpecificMatch = content
                    },

                },

            };
            return request;
        }

        public static CrawlRequest CreateRequestDocumentPartFile(string uri = null, string content = null)
        {
            var documentPartFile = new DocumentPartFile()
            {
                Selector = new DocumentPartSelector()
                {
                    Xpath ="//*[self::a or self::img]" ,
                    ContentSpecificMatch = content
                },
                BaseUri = uri,
            };

            var request = new CrawlRequest();
            request.LoadPageRequest = new LoadPageRequest{Uri = uri};
            request.RequestDocument = new Document()
            {
                RequestDocumentPart = documentPartFile,
                DownloadContent = true,
            };
            return request;
        }

        public static CrawlRequest CreateRequestDocumentPartArticle(string uri)
        {
            var request = new CrawlRequest();
            request.CrawlId = Guid.NewGuid();
            request.LoadPageRequest = new LoadPageRequest{Uri = uri};
            request.RequestDocument = new Document()
            {
                DownloadContent = true,
                RequestDocumentPart = new DocumentPartArticle()
                {
                    BaseUri = uri,
                    Title = new DocumentPartText()
                    {
                        BaseUri = uri,
                        Selector = new DocumentPartSelector()
                        {
                            Xpath = "//*[@class='titleClass']"
                        }
                    },
                    Content = new DocumentPartText()
                    {
                        BaseUri = uri,
                        Selector = new DocumentPartSelector()
                        {
                            Xpath = "//*[@class='content']"
                        },
                        SubParts = new List<DocumentPart>()
                        {
                            // Select all images within content
                            new DocumentPartFile
                            {
                                BaseUri = uri,
                                Selector = new DocumentPartSelector
                                {
                                    Xpath=".//img"
                                }
                            },
                            // Select all links within content
                            new DocumentPartLink
                            {
                                BaseUri = uri,
                                Selector = new DocumentPartSelector
                                {
                                    Xpath=".//a"
                                }
                            },
                            new DocumentPartTable{
                                BaseUri = uri
                            }
                        }
                    }

                },

            };
            return request;
        }

        public static CrawlRequest CreateRequestDocumentAutoDetect(string uri)
        {
            var documentPartAutoDetect = new DocumentPartAutodetect
            {
                BaseUri = uri
            };

            var request = new CrawlRequest();
            request.CrawlId = Guid.NewGuid();
            request.LoadPageRequest = new LoadPageRequest{Uri = uri};
            request.RequestDocument = new Document()
            {
                RequestDocumentPart = documentPartAutoDetect,
                DownloadContent = true,
            };
            return request;
        }


        internal static R GetResult<T, R>(TestCase<T> testcase) where R : DocumentPart
        {
            var testee = testcase.RequestDocument.RequestDocumentPart.Match(d => d, () => throw new Exception("Doc Part Missing"));
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(testcase.Xml);

            testee.Parse(htmlDocument).Match(y => y, () => Unit.Default);

            return testee as R;
        }
    }
}