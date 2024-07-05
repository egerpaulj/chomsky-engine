FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY bin/Release/net8.0/publish/ App/
WORKDIR /App
ENV ASPNETCORE_ENVIRONMENT=Production
COPY configserver.pfx /opt/certs/certificate.pfx
COPY exportphrase /opt/certs/exportphrase
EXPOSE 443
ENTRYPOINT ["./Crawler.Configuration.Server"]
