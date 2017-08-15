#!/bin/sh

# Deploy our script code to the storage bucket
# This makes the script code available to Cloud Functions

gcloud=`./commands/gcloud.sh` 

if [ "$#" -lt 1 ]; then
  echo "Usage: deploy_functions.sh <storage bucket name>"
  exit 1
fi

"$gcloud" beta functions deploy appendToLog --trigger-http --local-path=functions "--stage-bucket=$1"