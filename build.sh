#!/bin/bash
set -e
set +x

trap "cd $(pwd -P)" EXIT
cd "$(dirname "$0")"

if [[ ($1 == '--help') || ($1 == '-h') ]]; then
  echo "usage: build.sh <command>"
  echo "commands:"
  echo "  --build-driver        - clone microsoft/playwright into .upstream/ at the commit in"
  echo "                          src/Common/Version.props (DriverVersion), and build the driver from it"
  echo "  --generate            - generate the API and the transport channels from .upstream/"
  echo "  --roll <ref>          - roll to a microsoft/playwright branch, tag or commit: set DriverVersion,"
  echo "                          build the driver and generate"
  echo "  --changes [<from>]    - list the upstream commits to port since <from> (default: DriverVersion at HEAD)"
  echo "  --help                - show this help"
  echo
  exit 0
fi

if [[ $# == 0 ]]; then
  echo "missing command!"
  echo "try './build.sh --help' for more information"
  exit 1
fi

upstream_url="https://github.com/microsoft/playwright.git"
# The microsoft/playwright clone at DriverVersion (gitignored). --build-driver manages it.
upstream_repo_path=".upstream"
version_props_path="./src/Common/Version.props"

# The upstream paths that can change the .NET client: the API docs, the JS client that
# the .NET client ports, the code that the JS client shares with the server, the
# assertions and the wire protocol.
client_paths=(
  'docs/src/api/'
  'packages/playwright-core/src/client/'
  'packages/isomorphic/'
  'packages/playwright/src/matchers/matchers.ts'
  'packages/protocol/spec/'
)

function read_driver_version() {
  sed -n 's|.*<DriverVersion>\(.*\)</DriverVersion>.*|\1|p'
}

function build_driver() {
  echo "building driver..."
  dotnet run --project ./src/tools/Playwright.Tooling/Playwright.Tooling.csproj -- build-driver --basepath .
  echo "done"
}

function require_upstream() {
  local driver_version=$(read_driver_version < "${version_props_path}")
  if [[ "$(git -C "${upstream_repo_path}" rev-parse HEAD 2>/dev/null)" != "${driver_version}" ]]; then
    echo "${upstream_repo_path}/ is not at ${driver_version}, the commit in ${version_props_path}. Run: ./build.sh --build-driver"
    exit 1
  fi
}

function generate() {
  # The generators are upstream. PW_SRC_DIR runs them from another microsoft/playwright
  # checkout (with `npm ci` done), to try a change to a generator or to the docs. The
  # committed files must come from .upstream/: CI generates them again and fails when
  # they differ.
  local generator_repo_path="${upstream_repo_path}"
  if [[ -n "$PW_SRC_DIR" ]]; then
    generator_repo_path="$PW_SRC_DIR"
    echo "Generating from ${generator_repo_path}, not from ${upstream_repo_path}/"
  else
    require_upstream
  fi
  echo "Generating API..."
  rm -rf src/Playwright/API/Generated/
  node "$generator_repo_path/utils/doclint/generateDotnetApi.js" "src/Playwright"
  echo "Generating transport channels..."
  rm -rf "src/Playwright/Transport/Protocol/Generated/"
  node "$generator_repo_path/utils/generate_dotnet_channels.js" "src/Playwright"
  echo "Formatting source code..."
  dotnet format src/Playwright
  echo "done"
}

# Prints the commit of a microsoft/playwright branch, tag or commit.
function resolve_ref() {
  local ref="$1"
  if [[ "${ref}" =~ ^[0-9a-f]{40}$ ]]; then
    echo "${ref}"
    return
  fi
  # "^{}" is the commit that an annotated tag points to.
  local names=("refs/tags/${ref}^{}" "refs/tags/${ref}" "refs/heads/${ref}")
  local refs=$(git ls-remote "${upstream_url}" "${names[@]}")
  local name
  for name in "${names[@]}"; do
    local commit=$(echo "${refs}" | awk -v name="${name}" '$2 == name { print $1 }')
    if [[ -n "${commit}" ]]; then
      echo "${commit}"
      return
    fi
  done
  echo "${ref} is not a branch, a tag or a full commit of ${upstream_url}" >&2
  return 1
}

function roll() {
  if [[ -z "$1" ]]; then
    echo "missing <ref>: a microsoft/playwright branch (main, release-1.66), tag (v1.66.0) or commit"
    exit 1
  fi
  local old_driver_version=$(read_driver_version < "${version_props_path}")
  local new_driver_version
  new_driver_version=$(resolve_ref "$1")
  # The driver build bundles the latest release of this major version.
  local new_node_major_version=$(curl -fsSL "https://nodejs.org/dist/index.json" \
    | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>console.log(JSON.parse(s).find(r=>r.lts).version.slice(1).split('.')[0]))")
  if [[ -z "${new_node_major_version}" ]]; then
    echo "Failed to determine the Node.js major version for the driver"
    exit 1
  fi
  echo "Rolling from microsoft/playwright ${old_driver_version} to ${new_driver_version} (Node.js ${new_node_major_version})..."

  local xml_file_contents=$(cat "${version_props_path}")
  xml_file_contents=$(echo "${xml_file_contents}" | sed "s|<DriverVersion>.*</DriverVersion>|<DriverVersion>${new_driver_version}</DriverVersion>|")
  xml_file_contents=$(echo "${xml_file_contents}" | sed "s|<DriverNodeMajorVersion>.*</DriverNodeMajorVersion>|<DriverNodeMajorVersion>${new_node_major_version}</DriverNodeMajorVersion>|")
  echo "${xml_file_contents}" > "${version_props_path}"

  # The channels generator needs the dependencies that the driver build installs.
  build_driver
  generate

  local upstream_package_version=$(node -e "console.log(require('./${upstream_repo_path}/packages/playwright-core/package.json').version)")
  echo "Rolled to microsoft/playwright ${new_driver_version}; the driver reports version ${upstream_package_version}"
}

function changes() {
  require_upstream
  local new_driver_version=$(read_driver_version < "${version_props_path}")
  local old_driver_version="$1"
  if [[ -z "${old_driver_version}" ]]; then
    old_driver_version=$(git show "HEAD:${version_props_path}" | read_driver_version)
  fi
  if [[ "${old_driver_version}" == "${new_driver_version}" ]]; then
    echo "DriverVersion is ${new_driver_version} at HEAD too; pass the commit to compare with."
    exit 1
  fi
  # A commit that no branch or tag reaches any more, for example on a deleted branch.
  git -C "${upstream_repo_path}" cat-file -e "${old_driver_version}^{commit}" 2>/dev/null \
    || git -C "${upstream_repo_path}" fetch --quiet origin "${old_driver_version}"
  git -C "${upstream_repo_path}" --no-pager log --oneline --reverse "${old_driver_version}..${new_driver_version}" -- "${client_paths[@]}"
}


CMD="$1"
if [[ ("$CMD" == "--roll") ]]; then
  roll "$2"
elif [[ ("$CMD" == "--build-driver") ]]; then
  build_driver
elif [[ ("$CMD" == "--generate") ]]; then
  generate
elif [[ ("$CMD" == "--changes") ]]; then
  changes "$2"
else
  echo "ERROR: unknown command - $CMD"
  echo "Pass --help for supported commands"
  exit 1
fi
