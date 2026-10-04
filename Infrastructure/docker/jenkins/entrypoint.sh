#!/bin/bash
# Wormhole entrypoint: grants the jenkins user access to the docker socket that
# is bind-mounted from the host (docker.sock "wormhole") before dropping
# privileges from root down to the jenkins user.
set -euo pipefail

if [ -S /var/run/docker.sock ]; then
  DOCKER_GID="$(stat -c '%g' /var/run/docker.sock)"

  if ! getent group docker >/dev/null 2>&1; then
    groupadd -g "${DOCKER_GID}" docker
  fi

  usermod -aG docker jenkins
fi

exec gosu jenkins /usr/local/bin/jenkins.sh "$@"
