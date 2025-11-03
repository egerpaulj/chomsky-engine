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
using LanguageExt;

namespace Crawler.Core.Results
{
    public class CrawlResponseData
    {
        public static IReadOnlyList<string> WhiteList =
        [
            "https://chomsky.info",
            "http://chomsky.info",
            "chomsky.info",
            "https://www.amnesty.org",
            "http://www.amnesty.org",
            "www.amnesty.org",
            "https://www.cdc.gov",
            "http://www.cdc.gov",
            "www.cdc.gov",
            "https://www.theguardian.com",
            "http://www.theguardian.com",
            "www.theguardian.com",
            "https://www.medialens.org",
            "http://www.medialens.org",
            "www.medialens.org",
            "https://fair.org",
            "http://fair.org",
            "fair.org",
            "https://www.undocs.org",
            "https://undocs.org",
            "http://undocs.org",
            "http://www.undocs.org",
            "www.undocs.org",
            "undocs.org",
            "https://main.un.org",
            "http://main.un.org",
            "main.un.org",
            "https://press.un.org",
            "http://press.un.org",
            "press.un.org",
            "https://docs.un.org",
            "http://docs.un.org",
            "https://www.docs.un.org",
            "http://www.docs.un.org",
            "www.docs.un.org",
            "docs.un.org",
        ];
    }

    public class CrawlResponse
    {
        public Option<DateTime> Timestamp { get; set; }
        public Option<string> Uri { get; set; }
        public Option<Guid> CorrelationId { get; set; }
        public Option<Guid> CrawlerId { get; set; }

        public Option<Document> Result { get; set; }

        public Option<string> Raw { get; set; }
        public bool ShouldIndex { get; set; }
    }
}
