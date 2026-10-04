#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

WEBSITES=(
  "https://www.example.com"
)

RANDOM_INDEX=$(( RANDOM % ${#WEBSITES[@]} ))
RANDOM_WEBSITE="${WEBSITES[$RANDOM_INDEX]}"

URI="${1:-$RANDOM_WEBSITE}"
BASE_URI="${2:-$URI}"
PROVIDE_RAW="${3:-false}"
DOWNLOAD="${4:-false}"

echo "Starting crawl for: $URI (base: $BASE_URI)"

dotnet run -- start-crawl "$URI" "$BASE_URI" "$PROVIDE_RAW" "$DOWNLOAD"

