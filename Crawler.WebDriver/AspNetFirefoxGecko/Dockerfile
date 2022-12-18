FROM mcr.microsoft.com/dotnet/aspnet:6.0

# setup Firefox with headless support, and install the geckodriver
RUN apt-get update -y
RUN apt-get install firefox-esr -y
RUN apt-get install xvfb -y
RUN firefox --version

# gecko 0.31.0 is compatible with Firefox 91.13.0esr
COPY geckodriver /bin/.