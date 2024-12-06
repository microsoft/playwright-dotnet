#!/bin/bash
set -e
set +x

if [[ ($1 == '--help') || ($1 == '-h') || ($1 == '') || ($2 == '') ]]; then
  echo "usage: $(basename $0) {--arm64,--amd64} {jammy,noble} {8.0,9.0} playwright:localbuild-noble"
  echo
  echo "Build Playwright docker image and tag it as 'playwright:localbuild-noble'."
  echo "Once image is built, you can run it with"
  echo ""
  echo "  docker run --rm -it playwright:localbuild-noble /bin/bash"
  echo ""
  echo "NOTE: this requires on Playwright dependencies to be installed with 'npm install'"
  echo "      and Playwright itself being built with 'npm run build'"
  echo ""
  exit 0
fi

function cleanup() {
  rm -rf dist/
}

trap "cleanup; cd $(pwd -P)" EXIT
cd "$(dirname "$0")"

DOCKER_PLATFORM=""
DOTNET_ARCH=""
if [[ "$1" == "--arm64" ]]; then
  DOCKER_PLATFORM="linux/arm64";
  DOTNET_ARCH="linux-arm64";
elif [[ "$1" == "--amd64" ]]; then
  DOCKER_PLATFORM="linux/amd64"
  DOTNET_ARCH="linux-amd64"
else
  echo "ERROR: unknown platform specifier - $1. Only --arm64 or --amd64 is supported"
  exit 1
fi

# Set default .NET version to net8 if not specified
DOTNET_VERSION="$4"
if [[ "$DOTNET_VERSION" != "8.0" && "$DOTNET_VERSION" != "9.0" ]]; then
  echo "ERROR: unknown .NET version - $DOTNET_VERSION. Must be either '8.0' or '9.0'"
  exit 1
fi

dotnet publish ../../src/Playwright -o dist/ --arch $DOTNET_ARCH

docker build --progress=plain --platform "${DOCKER_PLATFORM}" --build-arg DOTNET_VERSION="${DOTNET_VERSION}" -t "$3" -f "Dockerfile.$2" .
