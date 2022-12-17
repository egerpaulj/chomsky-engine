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
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using Microservice.Serialization;
using Microservice.TestHelper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Caching.Redis.IntegrationTest
{
    [TestClass]
    public class RedisCacheTests
    {

        private RedisCacheProvider _testee;

        [TestInitialize]
        public void Setup()
        {
            _testee = new RedisCacheProvider(Mock.Of<ILogger<RedisCacheProvider>>(), new RedisConfiguration(TestHelper.GetConfiguration()), Mock.Of<IJsonConverterProvider>());
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task StoreStrType_WhenResultRequest_ResultObtainedFromCache()
        {
            var key = "TestKey";
            var data = $"I am some test data: {Guid.NewGuid()}";

            await _testee.StoreInCache(key, data)
            .Bind<Unit, string>(_ => _testee.Get<string>(key))
            .Match(
                res => Assert.IsTrue(data.Equals(res)),
                () => Assert.Fail("Failed to get value"),
                ex => throw ex);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task StoreDateTimeType_WhenResultRequest_ResultObtainedFromCache()
        {
            var key = "TestKey";
            var data = DateTime.Now;

            await _testee.StoreInCache(key, data)
            .Bind<Unit, DateTime>(_ => _testee.Get<DateTime>(key))
            .Match(
                res => Assert.AreEqual(0, data.Subtract(res).TotalMilliseconds),
                () => Assert.Fail("Failed to get value"),
                ex => throw ex);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task StoreDoubleType_WhenResultRequest_ResultObtainedFromCache()
        {
            var key = "TestKey";
            var data = 12.1231;

            await _testee.StoreInCache(key, data)
            .Bind<Unit, double>(_ => _testee.Get<double>(key))
            .Match(
                res => Assert.IsTrue((data - res) <= double.Epsilon),
                () => Assert.Fail("Failed to get value"),
                ex => throw ex);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task StoreObjectType_WhenResultRequest_ResultObtainedFromCache()
        {
            var key = "TestKey";
            var data = new TestData
            {
                Id = Guid.NewGuid(),
                Data = "I am some test data"

            };

            await _testee.StoreInCache(key, data)
            .Bind<Unit, TestData>(_ => _testee.Get<TestData>(key))
            .Match(
                res => Assert.AreEqual(data, res),
                () => Assert.Fail("Failed to get value"),
                ex => throw ex);
        }

        [TestMethod]
        [TestCategory("IntegrationTest")]
        public async Task LoadTest_ResultsObtainedFromCache()
        {
            int numberOfParallelRequests = 7000;
            var tasks = new List<Task<Unit>>();
            for (int i = 0; i < numberOfParallelRequests; i++)
            {
               tasks.Add( RunLoadTest(i));
            }

            await Task.WhenAll<Unit>(tasks.ToArray());

            for (int i = 0; i < numberOfParallelRequests; i++)
            {
               await _testee.Get<TestData>(GetKey(i)).Match(
                   res => Assert.AreEqual(GetData(i), res.Data),
                   () => throw new Exception("Test failed")
               );
            }
        }

        private Task<Unit> RunLoadTest(int index)
        {
            var key = GetKey(index);
            var data = new TestData
            {
                Id = Guid.NewGuid(),
                Data = GetData(index)

            };

            return _testee.StoreInCache(key, data, 60)
            .Match(
                res => res,
                () => Unit.Default,
                ex => throw ex);
        }

        private static string GetData(int index)
        {
            return $"I am some test data: {index}";
        }

        private static string GetKey(int index)
        {
            return $"TestKey{index}";
        }
    }
}
