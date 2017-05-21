#!/bin/sh

# Reference: https://chengl.com/working-with-multiple-projects-on-gke/

if [ $# -lt 4 ]; then
  echo "Usage: create_gcloud_configuration.sh <configuration name> <project name> <region> <zone>"
  exit 1
fi 

gcloud config configurations create $1
gcloud config set project $2
#gcloud config set functions/region $3
gcloud config set compute/region $3
gcloud config set compute/zone $4
