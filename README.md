# Centralized logging for Unity game clients

Stream debug logs from your game clients to [Google Cloud Platform](https://cloud.google.com/). Browse and search your game client logs in the [Stackdriver Logging](https://cloud.google.com/logging/) web UI.

This is primarily intended for use during internal development. If you use this in production, be aware that streaming all logs from all game clients can be costly.

This consists of three parts:
* An extension to [UberLogger](https://www.github.com/bbbscarter/UberLogger/) which regularly forwards logs from your client to Google Cloud Platform
* A small bit of JavaScript glue code which receives logs from game clients, and forwards it to Stackdriver
* The Stackdriver Logging web UI

# Setup

* Create a project in Google Cloud (see https://cloud.google.com/functions/docs/quickstart)
* Enable billing for the project (see https://cloud.google.com/functions/docs/quickstart)
* Enable Google Cloud Functions API (see https://cloud.google.com/functions/docs/quickstart)

* Create a gcloud project config with `./commands/create_gcloud_configuration.sh <configuration name> <project name>`
* Create a Cloud Storage bucket with appropriate name via `./commands/create_storage_bucket.sh <project name>`
* Deploy cloud function via `./commands/deploy_functions.sh <project name>` - note down the endpoint URL
* Test posting something to the endpoint via `./commands/log_lines.sh <endpoint URL>` (verify with [Stackdriver Logging web UI](https://console.cloud.google.com/logs))

* Add the Frontend/Assets/* files to your Unity project
* Add a UberLoggerLogToStackdriver component to your project
* Update all endpoint URLs in UberLoggerLogToStackdriver component
* Test printing something (verify with [Stackdriver Logging web UI](https://console.cloud.google.com/logs))

# Filtering and browsing results

# Development

* For local dev, install the Cloud Functions emulator ( https://cloud.google.com/functions/docs/emulator )
  - install nvm
	follow instructions at https://github.com/creationix/nvm
  - install & switch to nodejs 6.9.1
	nvm install 6.9.1
  - install cloud functions emulator
	npm install @google-cloud/functions-emulator
  - Deploy scripts to cloud functions emulator via `./commands/deploy_functions_local_test.sh`
  - Post example lines to cloud functions emulator via `./commands/log_lines_local_test.sh`
