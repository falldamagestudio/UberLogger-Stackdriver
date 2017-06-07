#!/bin/sh

# Reference: https://chengl.com/working-with-multiple-projects-on-gke/

# Hardcoded region & zone to us-central1-a since currently (2017-06-07) Google Cloud Functions give
#   strange errors during deploy/run if run from other regions than us-central1

gcloud=`./commands/gcloud.sh`

region="us-central1"
zone="us-central1-a"

if [ "$#" -lt 2 ]; then
  echo "Usage: create_gcloud_configuration.sh <configuration name> <project name>"
  exit 1
fi 

"$gcloud" config configurations create "$1"
"$gcloud" config set project "$2"
"$gcloud" config set compute/region "$region"
"$gcloud" config set compute/zone "$zone"
