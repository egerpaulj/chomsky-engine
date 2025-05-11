//      Microservice Message Exchange Libraries for .Net C#
//      Copyright (C) 2024  Paul Eger

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
using Microservice.DataModel.Core;
using Microsoft.Extensions.Logging;

namespace Microservice.Exchange.Core.Polling;

public interface IPollingConsumerFactory
{
    IPollingConsumer<T> Create<T>(Func<TryOptionAsync<List<T>>> queryDataFunc, int intervalInMs);
}

public class PollingConsumerFactory(ILoggerFactory loggerFactory, string routingKey = "")
    : IPollingConsumerFactory
{
    readonly ILoggerFactory loggerFactory = loggerFactory;
    readonly string routingKey = routingKey;

    public IPollingConsumer<T> Create<T>(
        Func<TryOptionAsync<List<T>>> queryDataFunc,
        int intervalInMs
    )
    {
        return new PollingConsumer<T>(
            loggerFactory.CreateLogger<IConsumer<T>>(),
            queryDataFunc,
            intervalInMs,
            routingKey
        );
    }
}
