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
using Crawler.Configuration.Core;
using Crawler.Core.Management;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Strategy;
using Crawler.Management.Core.RequestHandling.Core;
using Crawler.RequestHandling.Core;
using Crawler.Stategies.Core;
using Microsoft.Extensions.Logging;

namespace Crawler.Strategies.General
{
    public class CrawlDomainOnlyContinuationStrategy : CrawlAllContinuationStrategy
    {
        public CrawlDomainOnlyContinuationStrategy(
            ILogger<ICrawlContinuationStrategy> logger,
            IRequestPublisher requestPublisher
        )
            : base(logger, requestPublisher) { }

        protected override IEnumerable<DocumentPartLink> Filter(
            DocumentPart documentPart,
            IEnumerable<DocumentPartLink> links
        )
        {
            var baseUri = documentPart.BaseUri.Match(
                u => u,
                () => throw new CrawlStrategyException("Document Part must has a Base Uri")
            );
            _logger.LogInformation($"Found links in {baseUri}: {links.Count()}");

            return links.Where(l =>
                l.Uri.Bind<bool>(u => IsWithinDomain(u, baseUri)).Match(t => t, false)
            );
        }

        private static bool IsWithinDomain(string foundUri, string baseUri)
        {
            var foundBaseUri = GetBaseUri(foundUri);
            var actualBaseUri = GetBaseUri(baseUri);
            return foundBaseUri == actualBaseUri;
        }

        private static string GetBaseUri(string uri)
        {
            return new Uri(uri.ToLowerInvariant().Replace("www.", string.Empty)).Host;
        }
    }
}
