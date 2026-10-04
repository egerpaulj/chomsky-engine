# Locked-down Jenkins controller with dotnet 10 SDK and the docker CLI so it
# can build/run the chomsky-engine images and orchestrate docker-compose.
FROM jenkins/jenkins:lts-jdk17

USER root

ENV DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    PATH="/usr/share/dotnet:${PATH}"

# --- Install .NET 10 SDK ---------------------------------------------------
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet \
    && rm -f /tmp/dotnet-install.sh

# --- Install Docker CLI (client only) + gosu -------------------------------
# The daemon itself is not run in this container: the host docker.sock is
# bind-mounted in (see docker-compose.cicd.yml), i.e. the "docker wormhole"
# pattern, so only the client tooling is required here.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg lsb-release gosu libicu-dev \
    && install -m 0755 -d /etc/apt/keyrings \
    && curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc \
    && chmod a+r /etc/apt/keyrings/docker.asc \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
        > /etc/apt/sources.list.d/docker.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends docker-ce-cli docker-compose-plugin \
    && rm -rf /var/lib/apt/lists/*

# --- Jenkins locked-down bootstrap -----------------------------------------
ENV JAVA_OPTS="-Djenkins.install.runSetupWizard=false" \
    CASC_JENKINS_CONFIG="/usr/share/jenkins/ref/casc.yaml"

COPY plugins.txt /usr/share/jenkins/ref/plugins.txt
RUN jenkins-plugin-cli --plugin-file /usr/share/jenkins/ref/plugins.txt

COPY casc.yaml /usr/share/jenkins/ref/casc.yaml
COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# Stay root here: entrypoint.sh adds the jenkins user to the host's docker
# group (dynamically resolved from the mounted socket) then execs as jenkins.
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
