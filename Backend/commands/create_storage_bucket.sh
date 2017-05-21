#!/bin/sh 

if [ $# -lt 2 ]; then
  echo "Usage: create_storage_bucket.sh <storage bucket name> <multi-regional location>"
  exit 1
fi  

gsutil mb -p `gcloud config get-value core/project` -l $2 gs://$1
