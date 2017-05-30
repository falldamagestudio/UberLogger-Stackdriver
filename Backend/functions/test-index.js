
// Tests for Google Cloud Functions plumbing

var chai = require('chai');
var app = require('./test-app');

var chaiHttp = require('chai-http');

var should = chai.should();

chai.use(chaiHttp);

describe('tests', function() {

	it('should reject non-POST requests', function(done) {
		chai.request(app)
			.put('/appendToLog')
			.send({entries: [ { message: "Hello message 1" }, { message: "Hello message 2" } ] })
			.end(function(err, res) {
				res.should.have.status(405);
				done();
			});
	});

	it('should reject empty bodies', function(done) {
		chai.request(app)
			.post('/appendToLog')
			.end(function(err, res) {
				res.should.have.status(400);
				done();
			});
	});

	it('should accept an array of well-formed responses', function(done) {
		chai.request(app)
			.post('/appendToLog')
			.send({entries: [ { message: "Hello message 1" }, { message: "Hello message 2" } ] })
			.end(function(err, res) {
				res.should.have.status(200);
				done();
			});
	});

});
