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
using LanguageExt;

namespace Crawler.Core.Cache
{
    public interface ICache
    {
        TryOptionAsync<Unit> StoreCrawlEnded(Option<Crawl> crawl);
        TryOptionAsync<Unit> UpdateCrawlCompleted(Option<Guid> crawlId);

        TryOptionAsync<Unit> StoreLastRequest(Option<string> uri);

        TryOptionAsync<DateTime> GetLastRequestTime(Option<string> uri);

        TryOptionAsync<bool> IsActiveDownload(Option<string> uri);
        TryOptionAsync<Unit> SetActiveDownload(Option<string> uri, bool downloadState);
    }
}
