using System.Collections.Generic;
using LanguageExt;

namespace Microservice.Exchange.Bertrand;

public class BertrandExchangeModel
{
    public string ExchangeName { get; set; }
    public List<BertrandExchangeControlModel> Consumers { get; set; }
    public List<BertrandExchangeControlModel> Publishers { get; set; }
    public List<BertrandExchangeControlModel> PublisherFilters { get; set; }
    public List<BertrandExchangeControlModel> TransformerFilters { get; set; }
}

public class BertrandExchangeControlModel
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public string RegistrationDate { get; set; }
}
public interface IBertrandExchangeStore
{
    TryOptionAsync<Unit> SaveExchange(BertrandExchangeModel model);
    TryOptionAsync<BertrandExchangeModel> GetExchange(string name);

    TryOptionAsync<bool> IsConsumerActive(string exchangeName, string consumerName);
}