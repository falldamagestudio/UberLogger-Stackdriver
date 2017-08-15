#!/bin/sh

# Run unit tests against local Cloud Functions emulator + Stackdriver backend
# You need to have the Cloud Functions emulator running, latest code deployed to the emulator, and an appropriate Google Cloud project configured
# This will result in a bunch of lines being printed to the Stackdriver backend

(cd functions && npm test)