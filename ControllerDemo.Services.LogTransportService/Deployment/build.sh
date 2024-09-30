#!/bin/bash

DEFAULT_PUBLISH_DIRECTORY=$(pwd)/publish
PUBLISH_DIRECTORY=${PUBLISH_DIRECTORY:-$DEFAULT_PUBLISH_DIRECTORY}

echo "Publishing to $PUBLISH_DIRECTORY"

BASEDIR=$(dirname "$BASH_SOURCE")
pushd "$BASEDIR"/..
dotnet clean
dotnet publish -c release -o "$PUBLISH_DIRECTORY" --runtime linux-arm
