using Crawler.DataModel.Scheduler;
using LanguageExt;

public class CrawlUri
{
    public Option<string> BaseUri { get; set; }
    public Option<string> Uri { get; set; }
    public UriType UriTypeId { get; set; }
}
