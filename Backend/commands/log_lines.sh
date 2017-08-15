
# Obsolete, broken

if [ "$#" -lt 1 ]; then
  echo "Usage: log_lines.sh <Google Cloud Function endpoint>"
  exit 1
fi 

curl -d '{"logName": "testSessionId", "entries": [ { "sessionId": "testSessionId", "message": "hello world 1", "sourceLocation": { "file": "file 1", "line": "12", "function": "function 1" } }, {"sessionId": "testSessionId", "message": "hello 7", "sourceLocation": { "file": "file 2", "line": "34", "function": "function 2" }, "severity": "ERROR", "callStack": [ { "file": "c:\\file3.cs", "line": "56", "function": "class3.function3" }, { "file": "c:\\file4.cs", "line": "78", "function": "class4.function4" } ] } ] }' -H 'content-type:application/json' "$1"