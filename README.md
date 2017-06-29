# Centralized logging for Unity game clients

Stream debug logs from your game clients to [Google Cloud Platform](https://cloud.google.com/). Browse and search your game client logs in the [Stackdriver Logging](https://cloud.google.com/logging/) web UI. See common errors with callstacks in the [Stackdriver Error Reporting](https://cloud.google.com/error-reporting/) web UI.

This is primarily intended for use during internal development. If you use this in production, be aware that streaming all logs from all game clients can be costly.

This consists of three parts:
* An extension to [UberLogger](https://www.github.com/bbbscarter/UberLogger/) which regularly forwards logs from your client to Google Cloud Platform
* A small bit of JavaScript glue code which receives logs from game clients, and forwards it to Stackdriver
* The Stackdriver Logging & Stackdriver Error Reporting web UIs

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

Visit the [Stackdriver Logging web UI](https://console.cloud.google.com/logs). Find a single line of text from the session that you are interested in. Expand the entry, and filter on the given logName. This gives you all logs for a single session.

# Reliability, performance and scalability

## Reliability

If there is a failure during posting to the Cloud Functions endpoint, the game clients will retry posting the same set of messages until it succeeds. With the current implementation this means that the game client will gradually consume more and more memory if the Cloud Function endpoint is down or not accepting the content. Temporary hiccups will result in entries with delayed timestamps but all messages will be delivered to the Stackdriver service.

## Performance

If a game client posts messages to the Cloud Function when it is not actively deployed anywhere, it will take Google Cloud functions approximately 10 seconds to deploy it and accept the payload. Once the Cloud Function is deployed, messages are processed immediately.
The Cloud Function does not buffer any messages.

The time between posting an exception message and seeing it in the Error Reporting view seems to be between 1 and 10 seconds when doing small-scale tests.

## Scalability

Each game client is by default configured to post any messages once every second. The Cloud Function takes 300ms to execute in the average case. Cloud Functions have [a quota](https://cloud.google.com/functions/quotas) of max 400c concurrent invocations. Stackdriver Logging has [a quota](https://cloud.google.com/logging/quota-policy) of max 500 write calls per second. The bottleneck is therefore with 500 concurrent clients spamming messages to the system non-stop, or with 150.000 concurrent clients that encounter errors once every 5 minutes.

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
