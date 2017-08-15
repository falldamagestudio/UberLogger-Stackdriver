#!/bin/sh

# Deploy scripts to local Cloud Functions emulator

(cd functions && functions deploy appendToLog --trigger-http)