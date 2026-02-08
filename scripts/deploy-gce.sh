#!/usr/bin/env bash
set -euo pipefail

required_vars=(
  GCP_PROJECT_ID
  GCP_REGION
  GCP_ARTIFACT_REPO
  GCP_VM_NAME
  GCP_VM_ZONE
  FREETOOL_IAP_JWT_AUDIENCE
)

for var in "${required_vars[@]}"; do
  if [[ -z "${!var:-}" ]]; then
    echo "Missing required env var: $var" >&2
    exit 1
  fi
done

if ! command -v gcloud >/dev/null 2>&1; then
  echo "gcloud is required" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "docker is required" >&2
  exit 1
fi

IMAGE_NAME="${IMAGE_NAME:-freetool-api}"
REMOTE_DIR="${REMOTE_DIR:-~/freetool}"
TAG="${TAG:-$(git rev-parse --short HEAD)}"
REGISTRY_HOST="${GCP_REGION}-docker.pkg.dev"
IMAGE_URI="${REGISTRY_HOST}/${GCP_PROJECT_ID}/${GCP_ARTIFACT_REPO}/${IMAGE_NAME}:${TAG}"

echo "Building image: ${IMAGE_URI}"
gcloud auth configure-docker "${REGISTRY_HOST}" --quiet
docker build --platform linux/amd64 -f src/Freetool.Api/Dockerfile -t "${IMAGE_URI}" .
docker push "${IMAGE_URI}"

tmp_env_file="$(mktemp)"
cat > "${tmp_env_file}" <<EOF
FREETOOL_IMAGE=${IMAGE_URI}
FREETOOL_IAP_JWT_AUDIENCE=${FREETOOL_IAP_JWT_AUDIENCE}
FREETOOL_ORG_ADMIN_EMAIL=${FREETOOL_ORG_ADMIN_EMAIL:-}
FREETOOL_DATA_ROOT=${FREETOOL_DATA_ROOT:-/mnt/freetool-data}
EOF

echo "Copying compose bundle to VM: ${GCP_VM_NAME}"
gcloud compute ssh "${GCP_VM_NAME}" --zone "${GCP_VM_ZONE}" --command "mkdir -p ${REMOTE_DIR}"
gcloud compute scp docker-compose.gce.yml "${tmp_env_file}" \
  "${GCP_VM_NAME}:${REMOTE_DIR}/" \
  --zone "${GCP_VM_ZONE}"

echo "Deploying containers on VM"
gcloud compute ssh "${GCP_VM_NAME}" --zone "${GCP_VM_ZONE}" --command "
  set -euo pipefail
  cd ${REMOTE_DIR}
  mv -f $(basename "${tmp_env_file}") .env
  source .env
  sudo mkdir -p \"${FREETOOL_DATA_ROOT}/freetool-db\" \"${FREETOOL_DATA_ROOT}/openfga\"
  sudo chmod 0777 \"${FREETOOL_DATA_ROOT}/freetool-db\" \"${FREETOOL_DATA_ROOT}/openfga\"
  sudo docker compose -f docker-compose.gce.yml --env-file .env pull
  sudo docker compose -f docker-compose.gce.yml --env-file .env up -d --remove-orphans
  sudo docker image prune -f
"

rm -f "${tmp_env_file}"

echo "Deployment finished: ${IMAGE_URI}"
