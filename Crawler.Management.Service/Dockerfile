FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY bin/Release/net6.0/publish/ App/
WORKDIR /App
COPY requestmanagercert.pem /usr/local/share/ca-certificates/requestmanager.crt
COPY configservercert.pem /usr/local/share/ca-certificates/configserver.crt
RUN update-ca-certificates
ENV ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 443
EXPOSE 7777
ENTRYPOINT ["./Crawler.Management.Service"]
