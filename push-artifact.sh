#/bin/bash


[[ -z "$SERVICE_ID" ]] && { echo "Must provide SERVICE_ID environment variable" ; exit 1; }
[[ -z "$GIT_REVISION" ]] && { echo "Must provide GIT_REVISION environment variable" ; exit 1; }
[[ -z "$ARTIFACT_FILE_PATH" ]] && { echo "Must provide ARTIFACT_FILE_PATH environment variable" ; exit 1; }
[[ -z "$API_ROOT_URL" ]] && { echo "Must provide API_ROOT_URL environment variable" ; exit 1; }

DEFAULT_ARTIFACT_ID=$(uuidgen)
ARTIFACT_ID=${ARTIFACT_ID:-$DEFAULT_ARTIFACT_ID}

curl --fail -X PUT -H "Content-Type: application/json" -d "{\"id\": \"$ARTIFACT_ID\", \"service_id\": \"$SERVICE_ID\", \"git_revision\": \"$GIT_REVISION\"}" "$API_ROOT_URL/service-types/$SERVICE_ID/artifacts/$ARTIFACT_ID"
curl --fail -X PUT -H "Content-Type: multipart/form-data" -F "file=@$ARTIFACT_FILE_PATH" $API_ROOT_URL/artifacts/$ARTIFACT_ID/file