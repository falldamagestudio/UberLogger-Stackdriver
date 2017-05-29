
// Imports the Google Cloud client library
const Logging = require('@google-cloud/logging');

// Your Google Cloud Platform project ID
const projectId = 'unity-log-to-stackdriver';

// Instantiates a client
const loggingClient = Logging({
  projectId: projectId
});

// The name of the log to write to
const logName = 'unity-log-to-stackdriver';
// Selects the log to write to
const log = loggingClient.log(logName);



function logMessage (logline, res)
{
	// The metadata associated with the entry
	const metadata = { resource: { type: 'global' } };
	// Prepares a log entry
	const entry = log.entry(metadata, logline);
	
	// Writes the log entry
	log.write(entry)
		.then(() => {
			console.log(`Logged: ${logline}`);
			res.status(200).send();
		})
		.catch((err) => {
			console.error('ERROR:', err);
			res.status(500).send({ error: err });
		});
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
		if (req.body.logline !== undefined)
		{
			console.log("Logging message from client: " + req.body.logline);
			logMessage(req.body.logline, res);
		}
		else
			res.status(400).send({ error: 'appendToLog must have a \'logline\' member in message body' });
	}
	else
		res.status(405).send({ error: 'appendToLog only supports POST' });
};