
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY bin/Release/net9.0/publish/ App/
WORKDIR /App
EXPOSE 5000
ENTRYPOINT ["./Microservice.Core"]
