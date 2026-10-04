FROM mcr.microsoft.com/dotnet/aspnet:10.0

# setup Firefox with headless support, and install the geckodriver
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates wget \
    && install -d -m 0755 /etc/apt/keyrings \
    && wget -q https://packages.mozilla.org/apt/repo-signing-key.gpg \
        -O /etc/apt/keyrings/packages.mozilla.org.asc \
    && printf '%s\n' \
        'deb [signed-by=/etc/apt/keyrings/packages.mozilla.org.asc] https://packages.mozilla.org/apt mozilla main' \
        > /etc/apt/sources.list.d/mozilla.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends firefox-esr xvfb \
    && rm -rf /var/lib/apt/lists/*
RUN firefox --version

# gecko 0.31.0 is compatible with Firefox 91.13.0esr
COPY geckodriver /bin/.

COPY bin/Release/net10.0/publish/ App/
WORKDIR /App
COPY webdriver.pfx /opt/certs/certificate.pfx
COPY exportphrase /opt/certs/exportphrase
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 443
ENTRYPOINT ["./Crawler.WebDriver.Grpc.Server"]
