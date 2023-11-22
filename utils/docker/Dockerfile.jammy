FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy

ARG DEBIAN_FRONTEND=noninteractive
ARG TZ=America/Los_Angeles
ARG DOCKER_IMAGE_NAME_TEMPLATE="mcr.microsoft.com/playwright/dotnet:v%version%-jammy"

# === INSTALL dependencies ===

RUN apt-get update && \
    # Feature-parity with node.js base images.
    apt-get install -y --no-install-recommends git openssh-client curl gpg && \
    # clean apt cache
    rm -rf /var/lib/apt/lists/* && \
    # Create the pwuser
    adduser pwuser

# === BAKE BROWSERS INTO IMAGE ===

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

# 1. Add tip-of-tree Playwright package to install its browsers.
#    The package should be built beforehand from tip-of-tree Playwright.
COPY ./dist/ /tmp/playwright-dotnet

# 2. Bake in browsers & deps.
#    Browsers will be downloaded in `/ms-playwright`.
#    Note: make sure to set 777 to the registry so that any user can access
#    registry.
RUN mkdir /ms-playwright && \
    /tmp/playwright-dotnet/playwright.ps1 install --with-deps && \
    /tmp/playwright-dotnet/playwright.ps1 mark-docker-image "${DOCKER_IMAGE_NAME_TEMPLATE}" && \
    # Workaround for https://github.com/microsoft/playwright/issues/27313
    # While the gstreamer plugin load process can be in-process, it ended up throwing
    # an error that it can't have libsoup2 and libsoup3 in the same process because
    # libgstwebrtc is linked against libsoup2. So we just remove the plugin.
    if [ "$(uname -m)" = "aarch64" ]; then \
        rm /usr/lib/aarch64-linux-gnu/gstreamer-1.0/libgstwebrtc.so; \
    else \
        rm /usr/lib/x86_64-linux-gnu/gstreamer-1.0/libgstwebrtc.so; \
    fi && \
    rm -rf /var/lib/apt/lists/* && \
    rm -rf /tmp/* && \
    chmod -R 777 /ms-playwright
