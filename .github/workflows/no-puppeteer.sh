#!/bin/sh

search_result=$(grep -r -i -n "password[=:]" $GITHUB_WORKSPACE/src)

if [[ $search_result ]]; then
    echo "Puppeteer found! \n $search_result"
    exit 1
else
    echo "What is a puppeteer?"
fi