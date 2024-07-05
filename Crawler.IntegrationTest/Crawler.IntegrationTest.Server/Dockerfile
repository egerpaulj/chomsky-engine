FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY bin/Test/net8.0/publish/ App/
WORKDIR /App
ENV ASPNETCORE_ENVIRONMENT=Test
EXPOSE 80
ENTRYPOINT ["./Crawler.IntegrationTest.Server"]
    