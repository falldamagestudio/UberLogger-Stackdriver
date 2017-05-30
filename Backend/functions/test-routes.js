
// Routing for standalone node.js app - used when running unit tests, but not when deploying to Google Cloud Functions

var express = require('express');
var router = express.Router();
var index = require('./index');

router.all('/appendToLog', index.appendToLog);

module.exports = router;
