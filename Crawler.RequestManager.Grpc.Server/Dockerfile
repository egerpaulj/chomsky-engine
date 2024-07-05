FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY bin/Release/net8.0/publish/ App/
WORKDIR /App
COPY requestmanager.pfx /opt/certs/certificate.pfx
COPY exportphrase /opt/certs/exportphrase
COPY webdrivercert.pem /usr/local/share/ca-certificates/webdriver.crt
RUN update-ca-certificates
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 443
ENTRYPOINT ["./Crawler.RequestManager.Grpc.Server"]
