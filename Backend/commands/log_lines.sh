if [ "$#" -lt 1 ]; then
  echo "Usage: log_lines.sh <Google Cloud Function endpoint>"
  exit 1
fi 

curl -d '{"logName": "testSessionId", "entries": [ { "sessionId": "testSessionId", "message": "hello world 1", "sourceLocation": { "file": "file 1", "line": "12", "function": "function 1" } }, {"sessionId": "testSessionId", "message": "hello world 2", "sourceLocation": { "file": "file 2", "line": "34", "function": "function 2" } } ] }' -H 'content-type:application/json' "$1"