FROM mcr.microsoft.com/dotnet/aspnet:6.0

RUN apt-get update -y
RUN apt-get install firefox-esr -y
RUN apt-get install xvfb -y
RUN firefox --version
# gecko 0.31.0
# 91.13.0esr

COPY geckodriver /bin/.
