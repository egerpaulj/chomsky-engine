# Jenkins CI/CD

The `docker-compose.cicd.yml` stack runs a locked-down Jenkins controller (with the
.NET 10 SDK and the Docker CLI baked in) that builds, tests, releases and maintains the
Chomsky Engine services. Jenkins is fully bootstrapped via
[Configuration as Code](/Infrastructure/docker/jenkins/casc.yaml) - no manual setup wizard.

## Starting Jenkins

Set the required secrets (do **not** commit real values - `.env` is tracked in this repo):

```bash
export JENKINS_ADMIN_PASSWORD='<choose-a-strong-password>'
export GITHUB_TOKEN='<github PAT with repo + admin:repo_hook scopes>'
```

Then bring the controller up alongside the existing dependency network:

```bash
docker network create development_network 2>/dev/null || true
docker compose -f docker-compose.cicd.yml up -d --build
```

Jenkins is available at `http://localhost:8080` (Blue Ocean at `/blue`). The docker
socket is bind-mounted into the container ("wormhole" pattern) so pipelines can build
images and drive `docker compose` against the host daemon without Docker-in-Docker.

## What gets provisioned automatically

- **Security**: local realm, signup disabled, matrix authorization (admin vs. authenticated), CSRF crumbs.
- **Appearance**: dark theme.
- **GitHub integration**: PAT credential (`github-token`), webhook management, commit status.
- **Dashboard**: a `chomsky-cicd-dashboard` view aggregating build status, test result trends and history for every pipeline below.

## Pipelines

| Job | Script | Trigger | Purpose |
|---|---|---|---|
| `chomsky-engine-pri` (multibranch) | [Jenkinsfile](/Jenkinsfile) | GitHub webhook (push/PR) + branch scan | Restore/build/test the solution, publish MSTest (`.trx`) results, set the GitHub commit status/badge. Covers both "on commit" and "on branch creation". |
| `release-images` | [ci/Jenkinsfile.release](/ci/Jenkinsfile.release) | Manual / on demand | Builds the latest .NET code into the crawler service images, `docker compose up` the stack, and cuts a GitHub release. |
| `webdriver-restart-hourly` | [ci/Jenkinsfile.webdriver-restart](/ci/Jenkinsfile.webdriver-restart) | Cron `H * * * *` | Restarts the `webdriver_server` compose service every hour. |
| `cleanup-crawler-images-daily` | [ci/Jenkinsfile.cleanup-images](/ci/Jenkinsfile.cleanup-images) | Cron `H 2 * * *` | Prunes old `registry:5000/crawler/*` images, keeping the latest 3 per service. |
| `cleanup-completed-crawl-uris-daily` | [ci/Jenkinsfile.cleanup-mongo](/ci/Jenkinsfile.cleanup-mongo) | Cron `H 3 * * *` | Deletes `crawl_uri` documents in MongoDB where `CompletedTimestamp` is set. |

## Build status badge

The GitHub badge in the [README](/README.md) points at the embeddable-build-status
endpoint for the `main` branch job: update the host/port if Jenkins is exposed under a
different URL than `http://localhost:8080`.
