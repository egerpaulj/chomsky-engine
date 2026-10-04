FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY bin/Release/net10.0/publish/ App/
WORKDIR /App
ENV ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 443
EXPOSE 7778
ENTRYPOINT ["./Crawler.Scheduler.Service"]
