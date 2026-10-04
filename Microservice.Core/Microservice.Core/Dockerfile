
FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY bin/Release/net10.0/publish/ App/
WORKDIR /App
EXPOSE 5000
ENTRYPOINT ["./Microservice.Core"]
