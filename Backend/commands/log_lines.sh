if [ "$#" -lt 1 ]; then
  echo "Usage: log_lines.sh <Google Cloud Function endpoint>"
  exit 1
fi 

curl -d '{"entries": [ { "message": "hello world 1" }, {"message": "hello world 2" } ] }' -H 'content-type:application/json' "$1"