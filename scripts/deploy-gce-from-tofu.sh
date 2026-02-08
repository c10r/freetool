#!/usr/bin/env bash
set -euo pipefail

INFRA_DIR="${INFRA_DIR:-infra/opentofu}"

if ! command -v tofu >/dev/null 2>&1; then
  echo "tofu is required" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required" >&2
  exit 1
fi

if [[ ! -d "${INFRA_DIR}" ]]; then
  echo "Infra directory not found: ${INFRA_DIR}" >&2
  exit 1
fi

pushd "${INFRA_DIR}" >/dev/null
output_json="$(tofu output -json)"
popd >/dev/null

export GCP_PROJECT_ID="$(jq -r '.project_id.value' <<<"${output_json}")"
export GCP_REGION="$(jq -r '.artifact_registry_location.value' <<<"${output_json}")"
export GCP_ARTIFACT_REPO="$(jq -r '.artifact_registry_repo_id.value' <<<"${output_json}")"
export GCP_VM_ZONE="$(jq -r '.vm_zone.value' <<<"${output_json}")"
export FREETOOL_IAP_JWT_AUDIENCE="$(jq -r '.iap_jwt_audience.value' <<<"${output_json}")"
export FREETOOL_DATA_ROOT="$(jq -r '.data_mount_path.value' <<<"${output_json}")"

MIG_NAME="$(jq -r '.managed_instance_group_name.value? // empty' <<<"${output_json}")"
if [[ -n "${MIG_NAME}" ]]; then
  export GCP_MIG_NAME="${MIG_NAME}"
  unset GCP_VM_NAME || true
else
  export GCP_VM_NAME="$(jq -r '.vm_name.value' <<<"${output_json}")"
fi

if [[ "${FREETOOL_IAP_JWT_AUDIENCE}" == "" || "${FREETOOL_IAP_JWT_AUDIENCE}" == "null" ]]; then
  echo "iap_jwt_audience output is empty. Set iap_jwt_audience in OpenTofu variables first." >&2
  exit 1
fi

if [[ -z "${FREETOOL_ORG_ADMIN_EMAIL:-}" ]]; then
  FREETOOL_ORG_ADMIN_EMAIL_FROM_TF="$(jq -r '.org_admin_email.value? // empty' <<<"${output_json}")"
  if [[ -n "${FREETOOL_ORG_ADMIN_EMAIL_FROM_TF}" ]]; then
    export FREETOOL_ORG_ADMIN_EMAIL="${FREETOOL_ORG_ADMIN_EMAIL_FROM_TF}"
  fi
fi

"$(dirname "$0")/deploy-gce.sh"
