#!/usr/bin/env bash
set -euo pipefail

# Determine script and project directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

BRANCH_NAME="${BRANCH_NAME:-$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "local")}"
BUILD_ID="${BUILD_ID:-$(git rev-parse --short HEAD 2>/dev/null || echo "1")}"

export BRANCH_NAME
export BUILD_ID

echo "=================================================="
echo "Publishing .NET microservices (Release mode)..."
echo "=================================================="

dotnet publish -c Release Crawler.Data/Crawler.Data.Service/Crawler.Data.Service.csproj
dotnet publish -c Release Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/Crawler.WebDriver.Grpc.Server.csproj
dotnet publish -c Release Crawler.RequestManager.Grpc.Server/Crawler.RequestManager.Grpc.Server.csproj
dotnet publish -c Release Crawler.Management.Service/Crawler.Management.Service.csproj
dotnet publish -c Release Crawler.Scheduler/Crawler.Scheduler.Service/Crawler.Scheduler.Service.csproj

echo "=================================================="
echo "Generating Certificates..."
echo "=================================================="

TEMP_CERT_DIR="$(mktemp -d)"
trap 'rm -rf "$TEMP_CERT_DIR"' EXIT

openssl rand -base64 32 > "$TEMP_CERT_DIR/passphrase"
openssl rand -base64 32 > "$TEMP_CERT_DIR/exportphrase"

cp "$TEMP_CERT_DIR/exportphrase" Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/exportphrase
cp "$TEMP_CERT_DIR/exportphrase" Crawler.RequestManager.Grpc.Server/exportphrase

openssl req -x509 -newkey rsa:4096 \
  -keyout "$TEMP_CERT_DIR/webdriverkey.pem" \
  -out "$TEMP_CERT_DIR/webdrivercert.pem" \
  -days 365 -nodes -batch \
  -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=webdriver_server"

openssl req -x509 -newkey rsa:4096 \
  -keyout "$TEMP_CERT_DIR/requestmanagerkey.pem" \
  -out "$TEMP_CERT_DIR/requestmanagercert.pem" \
  -days 365 -nodes -batch \
  -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=request_server"

openssl pkcs12 -export \
  -out Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/webdriver.pfx \
  -inkey "$TEMP_CERT_DIR/webdriverkey.pem" \
  -in "$TEMP_CERT_DIR/webdrivercert.pem" \
  -passout file:"$TEMP_CERT_DIR/exportphrase"

openssl pkcs12 -export \
  -out Crawler.RequestManager.Grpc.Server/requestmanager.pfx \
  -inkey "$TEMP_CERT_DIR/requestmanagerkey.pem" \
  -in "$TEMP_CERT_DIR/requestmanagercert.pem" \
  -passout file:"$TEMP_CERT_DIR/exportphrase"

cp "$TEMP_CERT_DIR/requestmanagercert.pem" Crawler.Management.Service/requestmanagercert.pem
cp "$TEMP_CERT_DIR/webdrivercert.pem" Crawler.RequestManager.Grpc.Server/webdrivercert.pem

echo "=================================================="
echo "Ensuring Docker network 'development_network' exists..."
echo "=================================================="

if ! docker network inspect development_network >/dev/null 2>&1; then
  echo "Creating external network development_network..."
  docker network create development_network
else
  echo "Network development_network already exists."
fi

echo "=================================================="
echo "Building and starting containers via docker compose..."
echo "Image tags: ${BRANCH_NAME}_${BUILD_ID}"
echo "=================================================="

docker compose \
  -f docker-compose.dependencies.yml \
  -f docker-compose.monitoring.yml \
  -f docker-compose.yml \
  up -d --build "$@"

