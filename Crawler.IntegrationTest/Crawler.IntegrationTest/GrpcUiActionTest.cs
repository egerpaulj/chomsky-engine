using System.Collections.Generic;
using System.Threading.Tasks;
using Crawler.Core.Requests;
using Crawler.Microservice.Core;
using Crawler.WebDriver.Core;
using Crawler.WebDriver.Grpc.Client;
using Crawler.WebDriver.Selenium.Firefox;
using Microservice.Grpc.Core;
using Microservice.TestHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Crawler.IntegrationTest
{
    [TestClass]
    public class GrpcUiActionTest
    {
        private readonly IWebDriverService _testee;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _uri;

        public GrpcUiActionTest()
        {
            var appConfig = TestHelper.GetConfiguration();
            var config = appConfig as IConfiguration;
            _uri = config.GetValue<string>("IntegrationTestServer");
            

            _loggerFactory = LoggerFactory.Create(b =>
            {
                b.AddSimpleConsole();
            });

            
            _testee = new GrpcWebDriverService(
                appConfig,
                _loggerFactory.CreateLogger<GrpcWebDriverService>(),
                new GrpcMetrics(),
                new JsonConverterProvider());
        }

        [TestMethod]
        public async Task UiActionsTest()
        {
            // use for local testing diredctly with gecko
            //var testee = new WebDriverServiceFirefox(new WebDriverMetrics(), _loggerFactory.CreateLogger<WebDriverServiceFirefox>(), true);


            var result = await _testee.LoadPage(new LoadPageRequest
            {
                Uri=_uri,
                UserActions = new List<Core.UserActions.UiAction>
                {
                    new Core.UserActions.UiAction
                    {
                        Type = Core.UserActions.UiAction.ActionType.Input,
                        XPath = "//input[@name='input']",
                        ActionData = "Input data for test"                        
                    },
                    new Core.UserActions.UiAction
                    {
                        Type = Core.UserActions.UiAction.ActionType.Checkbox,
                        XPath = "//input[@name='checkBox2']",
                        ActionData = "True"                        
                    },
                    new Core.UserActions.UiAction
                    {
                        Type = Core.UserActions.UiAction.ActionType.Radio,
                        XPath = "//input[@value='RadioInput2']",
                        ActionData = "True"                        
                    },
                    new Core.UserActions.UiAction
                    {
                        Type = Core.UserActions.UiAction.ActionType.Dropdown,
                        XPath = "//input[@name='dropdown']",
                        ActionData = "Dropdown 2"                       
                    },
                    new Core.UserActions.UiAction
                    {
                        Type = Core.UserActions.UiAction.ActionType.Click,
                        XPath = "//input[@type='submit']",
                    },
                    new Core.UserActions.UiAction
                    {
                        Type = Core.UserActions.UiAction.ActionType.Wait,
                        XPath = "//div[@name='result']",
                    },


                }
            }).Match(r => r, () => throw new System.Exception("UI Action Test Failed"), ex => throw ex);
            
            //testee.Dispose();
            

            Assert.IsTrue(result.Contains("Result is: Input data for test-RadioInput2--on-Dropdown 2"));
                                           
        }

        
    }
}