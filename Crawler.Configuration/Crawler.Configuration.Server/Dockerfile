FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY bin/Release/net9.0/publish/ App/
WORKDIR /App
ENV ASPNETCORE_ENVIRONMENT=Production
COPY configserver.pfx /opt/certs/certificate.pfx
COPY exportphrase /opt/certs/exportphrase
EXPOSE 443
ENTRYPOINT ["./Crawler.Configuration.Server"]
