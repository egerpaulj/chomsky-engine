# build custom aspnet environment with Firefox and Gecko first: Crawler.WebDriver/AspNetFirefoxGecko/createAspRuntimeWithGecko.sh
FROM aspnet:ff91gk31
COPY bin/Release/net8.0/publish/ App/
WORKDIR /App
COPY webdriver.pfx /opt/certs/certificate.pfx
COPY exportphrase /opt/certs/exportphrase
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 443
ENTRYPOINT ["./Crawler.WebDriver.Grpc.Server"]
