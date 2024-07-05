using System;
using System.Collections.Generic;
using LanguageExt;

namespace Microservice.Exchange.Core.Bertrand;

public interface IBertrandStateStore
{
    TryOptionAsync<Unit> StoreIncomingMessage(Option<Message<object>> message);
    TryOptionAsync<Unit> StoreInDeadletter(Option<Message<object>> message);
    TryOptionAsync<IEnumerable<Message<object>>> GetOutstandingMessages();
    TryOptionAsync<Unit> Delete(Option<Guid> id);
}