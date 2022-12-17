//      Microservice Cache Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2021  Paul Eger                                                                                                                                                                     
                                                                                                                                                                                                                   
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
using Microservice.Mongodb.Repo;
using Microservice.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace Microservice.Mongodb.Test
{
    [TestClass]
    public class IntegrationTest
    {
        private MongoDbRepository<TestDataModel> _testee;

        [TestInitialize]
        public void Setup()
        {
            var mongodbConfigMock = new Mock<IDatabaseConfiguration>();
            mongodbConfigMock.Setup(m => m.DatabaseName).Returns("IntegrationTest");
            mongodbConfigMock.Setup(m => m.DocumentName).Returns("IntegrationTestDocuments");

            _testee = new MongoDbRepository<TestDataModel>(Microservice.TestHelper.TestHelper.GetConfiguration(), mongodbConfigMock.Object, Mock.Of<IJsonConverterProvider>()  );

            Cleanup();
        }

        [TestMethod]
        public async Task StoreDocument_WhenDocumentQueriedWithId_ThenDocumentExists()
        {
            // ARRANGE
            var id = Guid.NewGuid();
            var testData = new TestDataModel()
            {
                Data = $"Some test information: {id}"
            };

            // ACT
            var result = await _testee.AddOrUpdate(testData).Match(r => r, () => throw new Exception("Test Failed to Add Or update document"));
            
            // ASSERT
            var resultDoc = await _testee.Get(result).Match(r => r, () => throw new Exception("Test Failed: failed to get document"));

            Assert.AreEqual(testData.Data, resultDoc.Data);
        }

        [TestMethod]
        public async Task StoreMultipleDocuments_WhenDocumentsQueriedWithFilter_ThenDocumentsExist()
        {
            // ARRANGE
            var id1 = Guid.NewGuid();
            var testData1 = new TestDataModel()
            {
                Data = $"Some test information: {id1}"
            };

            var id2 = Guid.NewGuid();
            var testData2 = new TestDataModel()
            {
                Data = $"Some test information: {id2}"
            };

            // ACT
            var result1 = await _testee.AddOrUpdate(testData1).Match(r => r, () => throw new Exception("Test Failed to Add Or update document"));
            var result2 = await _testee.AddOrUpdate(testData2).Match(r => r, () => throw new Exception("Test Failed to Add Or update document"));
            
            // ASSERT
            var resultDoc = await _testee.GetMany(SelectAllFilter()).Match(r => r, () => throw new Exception("Test Failed: failed to get document"));

            Assert.AreEqual(2, resultDoc.Count);
            Assert.AreEqual(testData1.Id, resultDoc[0].Id);
            Assert.AreEqual(testData2.Id, resultDoc[1].Id);
            Assert.AreEqual(testData1.Data, resultDoc[0].Data);
            Assert.AreEqual(testData2.Data, resultDoc[1].Data);
        }

        [TestMethod]
        public async Task StoreMultipleDocuments_DeleteSingleDocumentWithId_ThenDocumentDoesNotExist()
        {
            // ARRANGE
            var id1 = Guid.NewGuid();
            var testData1 = new TestDataModel()
            {
                Data = $"Some test information: {id1}"
            };

            var id2 = Guid.NewGuid();
            var testData2 = new TestDataModel()
            {
                Data = $"Some test information: {id2}"
            };

            // ACT
            var result1 = await _testee.AddOrUpdate(testData1).Match(r => r, () => throw new Exception("Test Failed to Add Or update document"));
            var result2 = await _testee.AddOrUpdate(testData2).Match(r => r, () => throw new Exception("Test Failed to Add Or update document"));

            await _testee.Delete(result2).Match(r => r, () => throw new Exception("Test failed to delete data"));
            
            // ASSERT
            var resultDoc = await _testee.GetMany(SelectAllFilter()).Match(r => r, () => throw new Exception("Test Failed: failed to get document"));

            Assert.AreEqual(1, resultDoc.Count);
            Assert.AreEqual(testData1.Id, resultDoc[0].Id);
            Assert.AreEqual(testData1.Data, resultDoc[0].Data);
        }

        [TestMethod]
        public async Task StoreMultipleDocuments_DeleteMultipleDocumentsWithFilter_ThenDocumentsDeleted()
        {
            // ARRANGE
            var id1 = Guid.NewGuid();
            var testData1 = new TestDataModel()
            {
                Data = $"Some test information: {id1}"
            };

            var id2 = Guid.NewGuid();
            var testData2 = new TestDataModel()
            {
                Data = $"Some test information: {id2}"
            };

            // ACT
            var result1 = await _testee.AddOrUpdate(testData1).Match(r => r, () => throw new Exception("Test Failed to Add Or update document"));
            var result2 = await _testee.AddOrUpdate(testData2).Match(r => r, () => throw new Exception("Test Failed to Add Or update document"));

            await _testee.Delete(SelectAllFilter()).Match(r => r, () => throw new Exception("Test failed to delete data"));
            
            // ASSERT
            var resultDoc = await _testee.GetMany(SelectAllFilter()).Match(r => r, () => throw new Exception("Test Failed: failed to get document"));

            Assert.AreEqual(0, resultDoc.Count);
        }

        [TestCleanup]
        public void Cleanup()
        {
            var filter = SelectAllFilter();

            _testee.Delete(filter).Match(r => r, () => Unit.Default);
        }

        private static FilterDefinition<BsonDocument> SelectAllFilter()
        {
            return Builders<BsonDocument>.Filter.Where(t => true);
        }
    }
}
