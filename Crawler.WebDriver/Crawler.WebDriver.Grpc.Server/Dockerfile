FROM aspnet:ff91gk31
COPY bin/Release/net6.0/publish/ App/
WORKDIR /App
COPY webdriver.pfx /opt/certs/certificate.pfx
COPY exportphrase /opt/certs/exportphrase
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 443
ENTRYPOINT ["./Crawler.WebDriver.Grpc.Server"]
