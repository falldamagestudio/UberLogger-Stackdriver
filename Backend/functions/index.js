
// Imports the Google Cloud client library
const Logging = require('@google-cloud/logging');

// Your Google Cloud Platform project ID
const projectId = process.env.GCLOUD_PROJECT;

// Instantiates a client
const loggingClient = Logging({
  projectId: projectId
});

// Convert a callstack to a multi-line text string in standard C# exception print-out format
function callStackToText (callStack)
{
	callStackLines = callStack.map(function(callStackEntry) {
		// Example:  at MyClass.MyMethod(type args) in c:\\project\\source.cs:line 27\n
		return "        at " + callStackEntry['function'] + " in " + callStackEntry['file'] + ":line " + callStackEntry['line'] + "\n";
	});
	
	callStackLine = callStackLines.join("");
	
	return callStackLine;
}

function regularEntryToStackdriverEntry (log, entry)
{
	// The metadata associated with the entry
	const metadata = { resource: { type: 'cloud_function', labels: { project_id: projectId, region: '', function_name: 'uberlogger-stackdriver-client'} }, sourceLocation: entry['sourceLocation'], severity: entry['severity'] };

	var messageWithCallStack = entry['message'];
	
	if (('callStack' in entry) && entry['callStack'] && (entry['callStack'].length > 0))
		messageWithCallStack += "\n" + callStackToText(entry['callStack']);

	const stackdriverEntry = log.entry(metadata, messageWithCallStack);

	return stackdriverEntry;
}

function regularEntriesToStackdriverEntries (log, entries)
{
	return entries.map(function(entry) {
			return regularEntryToStackdriverEntry(log, entry);
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
		try
		{
			//console.log("Input: ", req.body);
			
			// The name of the log to write to
			const logName = req.body.logName;
			// Selects the log to write to
			var log = loggingClient.log(logName);

			// Convert input json data to Stackdriver data structures
			var stackdriverEntries = regularEntriesToStackdriverEntries(log, req.body.entries);
	
			// Submit log entries to Stackdriver
			log.write(stackdriverEntries)
				.then(() => {
					console.log("Logged ", stackdriverEntries.length, " entries");
					res.status(200).send();
				})
				.catch((err) => {
					console.error('Internal error: ', err);
					res.status(500).send({ error: err });
				});
		}
		catch (err)
		{
			console.error('User error: ', err);
			res.status(400).send({ error: 'Malformed input'});
		}

	}
	else
		res.status(405).send({ error: 'appendToLog only supports POST' });
};