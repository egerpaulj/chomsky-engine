FROM mcr.microsoft.com/dotnet/aspnet:5.0
COPY bin/Release/net5.0/publish/ App/
WORKDIR /App
COPY requestmanagercert.pem /usr/local/share/ca-certificates/requestmanager.crt
COPY configservercert.pem /usr/local/share/ca-certificates/configserver.crt
RUN update-ca-certificates
ENV ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 443
EXPOSE 7777
ENTRYPOINT ["./Crawler.Scheduler.Service"]
