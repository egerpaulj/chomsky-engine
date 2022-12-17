# Microservice.Grpc

.NET library - Generic implementation of Google's GRPC for long-running request-reply services.

GRPC should be used for Microservices with long-running requests; for which the client needs a response. Otherwise a REST Service would suffice.

See https://grpc.io

Instead of creating several "Protos", by defining the GRPC contracts for each Request-Reply; the library provides a **generic GRPC implementation**.

The following are provided:
- Generic Grpc **Client**: **IRpcClient<T, R>**
- Generic Grpc **Service implementation**: **abstract class RpcServiceBase<T, R> : Rpc.RpcBase**
- Middlewear to setup Grpc
- Prometheus Metrics 

**T** - Request Type

**R** - Response Type

## GRPC Client

The client interface is defined below:

```
public interface IRpcClient<T, R>
{
    TryOptionAsync<R> Execute(T request, string serverAddress, Guid correlationId);
}
```

The client does the following:
1) serializes the request T to JSON
2) sends the serialized JSON using GRPC 
3) async waits for the response from the server
4) deserializes the response and return R

Serialization/desrialization uses **Newtonsoft.Json** 

See Microservice.Serialization to define custom JSON serialization/deserialization.

## GRPC Server

To run a GRPC the following are needed:

### 1) HTTPS  

See Microservice.Core (provides a library to host HTTPS kestrel services)

**Note:** Grpc requires HTTPS; the client should trust the certificate used in the Grpc service

### 2) The following references are needed in your ASP.NET project file (.csproj)

Create a WebApi microservice

```
dotnet new webapi -n NewProject.Server
```

Edit the NewProject.Server.csproj file and add the references:
- Microservice.Core
- Microservice.Serilization
- Microservice.Grpc.Core.csproj (this library)
- Grpc.AspNetCore
- LanguageExt.Core


e.g. 
```
  <ItemGroup>
    <ProjectReference Include="..\Microservice.Core\Microservice.Core.csproj" />
    <ProjectReference Include="..\Microservice.Grpc.Core\Microservice.Grpc.Core.csproj" />
    <ProjectReference Include="..\Microservice.Serialization\Microservice.Serialization.csproj" />
    <PackageReference Include="Grpc.AspNetCore" Version="2.34.0" />
    <PackageReference Include="LanguageExt.Core" Version="3.4.15" />
  </ItemGroup>
```

### 3) Implementation of **RpcServiceBase<T, R>**

```
protected abstract TryOptionAsync<R> Execute(Option<T> request);
```

Implement only the work to be done by the GRPC Server. 


E.g. GRPC that takes a request (**BusinessRequest**) and returns a response (**BusinessResponse**)

```
public class TestRpcService : RpcServiceBase<BusinessRequest, BusinessResponse>
{
    public TestRpcService(ILogger<Rpc.RpcBase> logger, IGrpcMetrics metrics) : base(logger, metrics)
    {
    }

    protected override TryOptionAsync<BusinessResponse> Execute(Option<BusinessRequest> request)
    {
        return request
            .ToTryOptionAsync()
            .Bind<BusinessRequest, BusinessResponse>(req => async () => await Task.FromResult(new BusinessResponse()));
    }
}
```

### 4) Bootstrap

In the service startup ensure HTTPS is configured (**UseKestrelHttps()**); and the implemented Grpcservice is registered (**AddRpc()** and **ConfigureGrpcService()**):

#### E.g. HTTPS setup

**UseKestrelHttps()**
```
Host.CreateDefaultBuilder(args)
                .SetupLogging()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseKestrelHttps()
                    .UseStartup<Startup>();
                });
```

#### E.g. Setup GRPC (ensure the implemented service is registered: TestRpcService example above)

**AddRpc()** and **ConfigureGrpcService()**
```
public void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<IGrpcMetrics, GrpcMetrics>();
    services.AddGrpc();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app
    .UseRouting()
    .ConfigureGrpcService<TestRpcService>();
}
```

## Microservice.Core

The Core library provides additional components to your service; if necessary. 

See Microservice.Core: see https://github.com/egerpaulj/Microservice.Core

E.g.

```
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app
    .UseRouting()
    .UseCustomSerilogRequestLogging()
    .SetupMetrics()
    .UseMiddleware<RequestDurationMetricsMiddlewear>()
    .ConfigureGrpcService<TestRpcService>();
}
```

### Metrics GRPC client/server 

Use **SetupMetrics()** to expose the Metrics to prometheus

![Screenshot: Grafana displaying Grpc Metrics](/Documentation/GrpcMontoring.png)


### TryOptionAsync<T>

The interface uses a Monad **TryOptionAsync<T>** to represent a return type. This allows the following encapsulation:
- An Async operation
- Potential Exceptions
- Successfully Result T is returned (otherwise Exception or Null is returned).

This allows better binding of pure functions. The callee can decide when to propagate the monad back; by calling **.Match**.

Additionally, the callee can also use **.Bind** to another TryOptionAsync; creating a flow of pure functions

E.g.
```
await _configurationRepository
          .GetConfiguration(uri)
          .Match(
              r => r, // Result found => return result
              CreateDefault(u), // Result is null => create a default value
              e => throw e // Exception => throw error or return a value; for error-cases
              );
```

Note: null checks are not necessary.

see https://github.com/louthy/language-ext

## Performance and caveats

The additional JSON Serialization/Deserialization is an overhead to Google's ProtoBuf. (~100ms more or less; depending on the data transferred/serialized)

But it might be worth the overhead:
- to help new GRPC users; to quickly build consistent, generic GRPC services
- to have one .NET Grpc service/client library and contract definition
- if only interested in the RPC solution; i.e. the contract management/promise/definition is a blackbox

## License

Copyright (C) 2021  Paul Eger

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.