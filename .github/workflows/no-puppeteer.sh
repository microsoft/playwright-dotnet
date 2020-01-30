#!/bin/sh

search_result=$(grep -r -i -n "puppeteer" $GITHUB_WORKSPACE/src)

if [[ $search_result ]]; then
    echo "Puppeteer found! \n $search_result"
    exit 1
else
    echo "What is a puppeteer?"
fi