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
			res.status(200).send();
		}
		else
			res.status(400).send({ error: 'appendToLog must have a \'logline\' member in message body' });
	}
	else
		res.status(405).send({ error: 'appendToLog only supports POST' });
};