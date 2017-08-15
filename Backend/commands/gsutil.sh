#!/bin/bash

# Locate & return appropriate 'gsutil' command

set -eu

# The Docker Bash terminal under Windows will not match 'gsutil' against 'gsutil.cmd'. Therefore we will try both gsutil and gsutil.cmd. 

if command -v "gsutil" > /dev/null 2>&1; then
  echo "gsutil"
  exit 0
elif command -v "gsutil.cmd" > /dev/null 2>&1; then
  echo "gsutil.cmd"
  exit 0
else
  >&2 echo "Cannot run 'gsutil' commands. Please ensure you have installed the Google Cloud SDK and added the gsutil binaries directory to your PATH."
  exit 1
fi
