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
using Crawler.Core.Parser.DocumentParts;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Crawler.Core.UnitTest
{
    [TestClass]
    public class DocumentPartTextTest
    {
        [TestMethod]
        public void ParseTagBasedTextSimpleTest()
        {
            var testcase = TestCaseFactory.CreateTestCaseTagBasedSimple();
            AssertResult(testcase);
        }

        [TestMethod]
        public void ParseContentBasedTextComplexTest()
        {
            var testcase = TestCaseFactory.CreateTestCaseContentBasedTextComplex();
            AssertResult(testcase);
        }

        [TestMethod]
        public void ParseAttributeBasedComplexTest()
        {
            var testcase = TestCaseFactory.CreateTestCaseAttributeBasedComplex();
            AssertResult(testcase);
        }

        [TestMethod]
        public void ParseAttributeAndContentBasedComplexTest()
        {
            var testcase = TestCaseFactory.CreateTestCaseAttributeAndContentBasedComplex();
            AssertResult(testcase);
        }

        private static void AssertResult(TestCase<string> testcase)
        {
            DocumentPartText result = DocumentPartTestHelper.GetResult<string, DocumentPartText>(testcase);
            Assert.IsNotNull(result);

            var textResult = result.Text.Match(t => t, () => throw new Exception("Empty result"));

            Assert.AreEqual(testcase.ExpectedResult, textResult);
        }
    }
}