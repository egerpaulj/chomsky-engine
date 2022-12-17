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
using System.Linq;
using Crawler.Core.Parser.DocumentParts;
using LanguageExt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Crawler.Core.UnitTest
{
    [TestClass]
    public class DocumentPartTableTest
    {
        [TestMethod]
        public void ParseTableTest()
        {
            var testcase = TestCaseFactoryTable.Create();
            DocumentPartTable result = DocumentPartTestHelper.GetResult<ExpectedTable, DocumentPartTable>(testcase);
            AssertResults(testcase, result);
        }

        public static void AssertResults(TestCase<ExpectedTable> testcase, DocumentPartTable result)
        {
            Assert.IsNotNull(result);

            
            AssertTable(testcase.ExpectedResult, result);
        }

        public static void AssertTable(ExpectedTable expectedResult, DocumentPartTable result)
        {
            var headers = result.Headers.Match(t => t, () => new List<DocumentPart>()).Cast<DocumentPart>().ToList();
            var rows = result.Rows.Match(t => t, () => new List<DocumentPartTableRow>()).Cast<DocumentPartTableRow>().ToList();
            
            Assert.AreEqual(expectedResult.Headers.Count, headers.Count);
            Assert.AreEqual(expectedResult.Rows.Count, rows.Count);


            for (var i = 0; i < headers.Count; i++)
            {
                Assert.AreEqual(expectedResult.Headers[i], GetText(headers[i]));
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var columns = rows[i].Columns.Match(t => t, () => throw new Exception("Empty result")).ToList();

                Assert.AreEqual(expectedResult.Rows[i].Content.Count, columns.Count);

                for (var j = 0; i < columns.Count; i++)
                {
                    Assert.AreEqual(expectedResult.Rows[j].Content[j], GetText(columns[j]));
                }
            }
        }

        private static Option<string> GetText(DocumentPart documentPart)
        {
            if (documentPart is DocumentPartText)
                return (documentPart as DocumentPartText).Text;
            if (documentPart is DocumentPartLink)
                return (documentPart as DocumentPartLink).Text;
            if (documentPart is DocumentPartArticle)
            {
                var article = documentPart as DocumentPartArticle;
                var content = article.Content.Match(c => c, () => new DocumentPartText());
                if (content is DocumentPartText)
                    return (article.Content.Match(c => c, () => new DocumentPartText()) as DocumentPartText).Text;
                return content.GetAllParts<DocumentPartText>().FirstOrDefault()?.Text ?? string.Empty;

            }

            return string.Empty;


        }
    }
}