#!/bin/sh 

# multi-regional bucket location hardcoded to match the hardcoded Cloud Functions location
# Bucket location taken from https://cloud.google.com/storage/docs/bucket-locations

gcloud=`./commands/gcloud.sh`
gsutil=`./commands/gsutil.sh`

multi_regional_location="us"

if [ "$#" -lt 1 ]; then
  echo "Usage: create_storage_bucket.sh <storage bucket name>"
  exit 1
fi  

"$gsutil" mb -p `"$gcloud" config get-value core/project` -l "$multi_regional_location" "gs://$1"
