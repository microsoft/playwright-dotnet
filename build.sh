#!/bin/bash
set -e
set +x

trap "cd $(pwd -P)" EXIT
cd "$(dirname "$0")"

if [[ ($1 == '--help') || ($1 == '-h') ]]; then
  echo "usage: build.sh <command>"
  echo "commands:"
  echo "  --roll <version>      - roll the .NET language binding to a specific driver version"
  echo "  --download-driver     - download the driver"
  echo "  --help                - show this help"
  echo
  exit 0
fi

if [[ $# == 0 ]]; then
  echo "missing command!"
  echo "try './build.sh --help' for more information"
  exit 1
fi

upstream_repo_path="../playwright"
if [[ -n "$PW_SRC_DIR" ]]; then
  upstream_repo_path="$PW_SRC_DIR"
fi
echo "Upstream repo path: ${upstream_repo_path}"

function download_driver() {
  echo "downloading driver..."
  dotnet run --project ./src/tools/Playwright.Tooling/Playwright.Tooling.csproj -- download-drivers --basepath .
  echo "done"
}

function roll_driver() {
  new_driver_version="$1"
  upstream_package_version=$(node -e "console.log(require('${upstream_repo_path}/package.json').version)")
  echo "Rolling .NET driver to driver ${new_driver_version} and upstream version ${upstream_package_version}..."

  xml_file_path="./src/Common/Version.props"
  xml_file_contents=$(cat "${xml_file_path}")
  xml_file_contents=$(echo "${xml_file_contents}" | sed "s|<DriverVersion>.*</DriverVersion>|<DriverVersion>${new_driver_version}</DriverVersion>|")
  echo "${xml_file_contents}" > "${xml_file_path}"

  echo "Generating API..."
  rm -rf src/Playwright/API/Generated/
  node "$upstream_repo_path/utils/doclint/generateDotnetApi.js" "src/Playwright"
  echo "Generating transport channels..."
  rm -rf "src/Playwright/Transport/Protocol/Generated/"
  node "$upstream_repo_path/utils/generate_dotnet_channels.js" "src/Playwright"
  echo "Formatting source code..."
  dotnet format src/Playwright

  download_driver

  pushd src/Playwright.TestingHarnessTest/
  echo "Updating driver version in tests..."
  npm install --save-exact "@playwright/test@${new_driver_version}"
  popd

  echo "done"
}

CMD="$1"
if [[ ("$CMD" == "--roll") ]]; then
  roll_driver $2
elif [[ ("$CMD" == "--download-driver") ]]; then
  download_driver
else
  echo "ERROR: unknown command - $CMD"
  echo "Pass --help for supported commands"
  exit 1
fi