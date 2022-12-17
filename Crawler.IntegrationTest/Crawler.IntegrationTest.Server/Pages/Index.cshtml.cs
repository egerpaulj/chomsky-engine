using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Crawler.IntegrationTest.Server.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public string Result{get;set;}

        public void OnGet()
        {

        }

        public void OnPost()                
        {
            var input = Request.Form["input"];
            var radio = Request.Form["radioInput"];
            var checkbox1 = Request.Form["checkbox1"];
            var checkbox2 = Request.Form["checkbox2"];
            var dropdown = Request.Form["dropdown"];

            

            Task.Delay(2000).Wait();

            Result = $"{input}-{radio}-{checkbox1}-{checkbox2}-{dropdown}";

        }
    }
}
