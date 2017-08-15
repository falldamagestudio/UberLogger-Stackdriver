
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

	it('should accept an entry without sourceLocation and with empty callstack', function(done) {

		entry = {
            sessionId: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			message: "Hello message, without sourceLocation, with empty callstack",
			severity: 200,
			"sourceLocation": null,
            "callStack": []
		}

		body = {
			logName: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			entries: [ entry ]
		}
	
		chai.request(app)
			.post('/appendToLog')
			.send(body)
			.end(function(err, res) {
				res.should.have.status(200);
				done();
			});
	});

	it('should accept an entry with sourceLocation but empty callstack', function(done) {

		entry = {
            sessionId: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			message: "Hello message, with sourceLocation, with empty callStack",
			severity: 200,
            "sourceLocation": {  
                "file":"/path/to/file.cs",
                "line":"103",
                "function":"ExampleClass.ExampleMethod(OtherExampleClass parameter)"
            },
            "callStack": [
            ]
		}

		body = {
			logName: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			entries: [ entry ]
		}
	
		chai.request(app)
			.post('/appendToLog')
			.send(body)
			.end(function(err, res) {
				res.should.have.status(200);
				done();
			});
	});

	it('should accept an entry with sourceLocation but null callstack', function(done) {

		entry = {
            sessionId: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			message: "Hello message, with sourceLocation, with null callStack",
			severity: 200,
            "sourceLocation": {  
                "file":"/path/to/file.cs",
                "line":"103",
                "function":"ExampleClass.ExampleMethod(OtherExampleClass parameter)"
            },
            "callStack": null
		}

		body = {
			logName: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			entries: [ entry ]
		}
	
		chai.request(app)
			.post('/appendToLog')
			.send(body)
			.end(function(err, res) {
				res.should.have.status(200);
				done();
			});
	});

	it('should accept an entry with sourceLocation and callstack', function(done) {

		entry = {
            sessionId: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			message: "Hello message, with sourceLocation, and callStack",
			severity: 200,
            "sourceLocation": {  
                "file":"/path/to/file.cs",
                "line":"103",
                "function":"ExampleClass.ExampleMethod(OtherExampleClass parameter)"
            },
            "callStack": [
				{
					"file":"/path/to/file.cs",
					"line":"103",
					"function":"ExampleClass.ExampleMethod(OtherExampleClass parameter)"
				},
				{
					"file":"/path/to/file2.cs",
					"line":"104",
					"function":"ExampleClass.ExampleMethod2(OtherExampleClass parameter)"
				}
            ]
		}

		body = {
			logName: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			entries: [ entry ]
		}
	
		chai.request(app)
			.post('/appendToLog')
			.send(body)
			.end(function(err, res) {
				res.should.have.status(200);
				done();
			});
	});

	it('should accept an error with sourceLocation and callstack', function(done) {

		entry = {
            sessionId: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			message: "Hello message, with sourceLocation, and callStack",
			severity: 500,
            "sourceLocation": {  
                "file":"/path/to/file.cs",
                "line":"103",
                "function":"ExampleClass.ExampleMethod(OtherExampleClass parameter)"
            },
            "callStack": [
				{
					"file":"/path/to/file.cs",
					"line":"103",
					"function":"ExampleClass.ExampleMethod(OtherExampleClass parameter)"
				},
				{
					"file":"/path/to/file2.cs",
					"line":"104",
					"function":"ExampleClass.ExampleMethod2(OtherExampleClass parameter)"
				}
            ]
		}

		body = {
			logName: "50f25e2f-e942-4cf4-a127-bc557074fd8b",
			entries: [ entry ]
		}
	
		chai.request(app)
			.post('/appendToLog')
			.send(body)
			.end(function(err, res) {
				res.should.have.status(200);
				done();
			});
	});
});
