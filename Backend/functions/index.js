
// Imports the Google Cloud client library
const Logging = require('@google-cloud/logging');

// Your Google Cloud Platform project ID
const projectId = process.env.GCLOUD_PROJECT;

// Instantiates a client
const loggingClient = Logging({
  projectId: projectId
});

// The name of the log to write to
const logName = process.env.GCLOUD_PROJECT;
// Selects the log to write to
const log = loggingClient.log(logName);


function regularEntryToStackdriverEntry (entry)
{
	// The metadata associated with the entry
	const metadata = { resource: { type: 'global' } };

	// Prepares a log entry
	const stackdriverEntry = log.entry(metadata, entry['message']);
	
	return stackdriverEntry;
}

function regularEntriesToStackdriverEntries (entries)
{
	return entries.map(regularEntryToStackdriverEntry);
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
			var stackdriverEntries = regularEntriesToStackdriverEntries(req.body.entries);
		}
		catch (err)
		{
			res.status(400).send({ error: 'malformed input; should be on the form of { entries: [ { message: "Entry 1 message", ... }, { message: "Entry 2 message", ... }, ... ] }'});
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