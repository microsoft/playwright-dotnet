#!/bin/bash
set -e
set +x

if [[ ($1 == '--help') || ($1 == '-h') || ($1 == '') || ($2 == '') ]]; then
  echo "usage: $(basename $0) {--arm64,--amd64} {noble,resolute} playwright:localbuild-noble"
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

dotnet publish ../../src/Playwright -o dist/ --arch $DOTNET_ARCH

# Keep each arch image a plain single-platform manifest without the unknown/unknown platform entry.
export BUILDX_NO_DEFAULT_ATTESTATIONS=1

# arm64 images are cross-built under QEMU user-mode emulation, where ldconfig
# segfaults intermittently at startup (tonistiigi/binfmt#298, every binfmt build
# since QEMU 8.1.4). apt's libc-bin trigger runs ldconfig, so a crash fails the
# whole `docker build`. Retry: BuildKit keeps the layers that already succeeded, so a
# retry re-runs only the failed RUN step.
MAX_ATTEMPTS=1
if [[ "${DOCKER_PLATFORM}" == "linux/arm64" ]]; then
  MAX_ATTEMPTS=3
fi

for ((attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)); do
  if docker build --progress=plain --platform "${DOCKER_PLATFORM}" \
      --build-arg UBUNTU_MIRROR_PREFIX="${UBUNTU_MIRROR_PREFIX}" \
      -t "$3" -f "Dockerfile.$2" .; then
    exit 0
  fi
  if (( attempt < MAX_ATTEMPTS )); then
    echo "docker build failed (attempt ${attempt}/${MAX_ATTEMPTS}), retrying..." >&2
  fi
done
echo "ERROR: docker build failed after ${MAX_ATTEMPTS} attempt(s)" >&2
exit 1
