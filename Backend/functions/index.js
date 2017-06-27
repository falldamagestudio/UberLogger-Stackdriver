
// Imports the Google Cloud client library
const Logging = require('@google-cloud/logging');

// Your Google Cloud Platform project ID
const projectId = process.env.GCLOUD_PROJECT;

// Instantiates a client
const loggingClient = Logging({
  projectId: projectId
});

function regularEntryToStackdriverEntry (log, entry)
{
	// The metadata associated with the entry
	const metadata = { resource: { type: 'global' }, sourceLocation: entry['sourceLocation'], severity: entry['severity'] };

	// Prepares a log entry
	const stackdriverEntry = log.entry(metadata, entry['message']);
	
	return stackdriverEntry;
}

function regularEntriesToStackdriverEntries (log, entries)
{
	return entries.map(entry => regularEntryToStackdriverEntry(log, entry));
}

/**
* HTTP Cloud Function.
*
* @param {Object} req Cloud Function request context.
* @param {Object} res Cloud Function response context.
*/
exports.appendToLog = function helloHttp (req, res) {
	if (req.method == 'POST')
	{
		try
		{
			// The name of the log to write to
			const logName = req.body.logName;
			// Selects the log to write to
			var log = loggingClient.log(logName);

			var stackdriverEntries = regularEntriesToStackdriverEntries(log, req.body.entries);
		}
		catch (err)
		{
			console.error('ERROR:', err);
			res.status(400).send({ error: 'malformed input; should be on the form of { logName: "session id", entries: [ { sessionId: "session id", message: "Entry 1 message", ... }, { sessionId: "session id", message: "Entry 2 message", ... }, ... ] }'});
		}

		log.write(stackdriverEntries)
			.then(() => {
				console.log('Logged entries; ', stackdriverEntries);
				res.status(200).send();
			})
			.catch((err) => {
				console.error('ERROR:', err);
				res.status(500).send({ error: err });
			});
	}
	else
		res.status(405).send({ error: 'appendToLog only supports POST' });
};