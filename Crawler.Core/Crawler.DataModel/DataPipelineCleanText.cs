using System;
using System.Collections.Generic;
using Microservice.DataModel.Core;
using Newtonsoft.Json;

public class DataPipelineCleanText : IDataModel
{
    [JsonProperty("src")]
    public string DataSource { get; set; }

    [JsonProperty("src_type")]
    public string DataSourceType { get; set; }

    [JsonProperty("text")]
    public string CleanedText { get; set; }

    public string Uri { get; set; }

    [JsonProperty("_id")]
    public Guid Id { get; set; }
    public string Created { get; set; }
    public string Updated { get; set; }

    [JsonProperty("entities")]
    public List<string> Entities { get; set; }
}
