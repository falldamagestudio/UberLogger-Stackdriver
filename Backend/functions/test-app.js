
// Standalone node.js app - used when running unit tests, but not when deploying to Google Cloud Functions

var express = require('express');
var bodyParser = require('body-parser');
var routes = require('./test-routes');

var app = express();

app.use(bodyParser.json());

app.use('/', routes);

app.listen(3000, function () {
  console.log('Example app listening on port 3000!')
});

module.exports = app;
