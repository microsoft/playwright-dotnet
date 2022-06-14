#!/bin/bash
set -e
set +x

if [[ ($1 == '--help') || ($1 == '-h') || ($1 == '') || ($2 == '') ]]; then
  echo "usage: $(basename $0) {--arm64,--amd64} {bionic,focal} playwright:localbuild-focal"
  echo
  echo "Build Playwright docker image and tag it as 'playwright:localbuild-focal'."
  echo "Once image is built, you can run it with"
  echo ""
  echo "  docker run --rm -it playwright:localbuild-focal /bin/bash"
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

# .NET internally has two sources when we install a tool with --add-source
# so our local version needs to be higher that it gets priority over the remote one.
# Also it should not include any pre-release versions (include next).
xml_file_path="../../src/Common/Version.props"
xml_file_contents=$(cat "${xml_file_path}")
xml_file_contents=$(echo "${xml_file_contents}" | sed "s|<AssemblyVersion>.*</AssemblyVersion>|<AssemblyVersion>1.99.99</AssemblyVersion>|")
xml_file_contents=$(echo "${xml_file_contents}" | sed "s|<PackageVersion>.*</PackageVersion>|<PackageVersion>1.99.99</PackageVersion>|")
echo "${xml_file_contents}" > "${xml_file_path}"

mkdir dist
dotnet build ../../src/Playwright
dotnet pack ../../src/Playwright -o dist/

PLATFORM=""
if [[ "$1" == "--arm64" ]]; then
  PLATFORM="linux/arm64";
elif [[ "$1" == "--amd64" ]]; then
  PLATFORM="linux/amd64"
else
  echo "ERROR: unknown platform specifier - $1. Only --arm64 or --amd64 is supported"
  exit 1
fi

docker build --progress=plain --platform "${PLATFORM}" -t "$3" -f "Dockerfile.$2" .
