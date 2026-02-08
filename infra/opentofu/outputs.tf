output "vm_name" {
  value       = "${var.name_prefix}-vm"
  description = "Base instance name used by the managed instance group"
}

output "vm_zone" {
  value       = var.zone
  description = "Compute instance zone"
}

output "vm_external_ip" {
  value       = null
  description = "Managed instances use ephemeral external IPs, so there is no stable VM external IP output"
}

output "managed_instance_group_name" {
  value       = google_compute_instance_group_manager.freetool.name
  description = "Managed instance group name"
}

output "load_balancer_ip" {
  value       = google_compute_global_address.freetool.address
  description = "Global IP for HTTPS load balancer"
}

output "https_url" {
  value       = "https://${var.domain_name}/"
  description = "Public Freetool URL"
}

output "artifact_registry_repo" {
  value       = google_artifact_registry_repository.freetool.name
  description = "Artifact Registry repository resource name"
}

output "project_id" {
  value       = var.project_id
  description = "GCP project ID used by infra"
}

output "artifact_registry_location" {
  value       = var.artifact_registry_location
  description = "Artifact Registry location used by infra"
}

output "artifact_registry_repo_id" {
  value       = var.artifact_registry_repo
  description = "Artifact Registry repository ID used by infra"
}

output "iap_jwt_audience" {
  value       = var.iap_jwt_audience
  description = "IAP JWT audience passed to API"
  sensitive   = true
}

output "org_admin_email" {
  value       = var.org_admin_email
  description = "Configured org admin email"
}

output "data_disk_name" {
  value       = var.preserve_data_disk_on_destroy ? google_compute_disk.data_protected[0].name : google_compute_disk.data_unprotected[0].name
  description = "Persistent data disk name"
}

output "data_mount_path" {
  value       = var.data_mount_path
  description = "Data disk mount path on VM"
}

output "iap_jwt_audience_hint" {
  value       = "/projects/<project-number>/global/backendServices/<backend-service-id>"
  description = "Format expected by Auth:IAP:JwtAudience (get backend-service-id from gcloud describe)."
}

output "backend_service_name" {
  value       = google_compute_backend_service.freetool.name
  description = "Backend service name for gcloud describe lookups"
}

output "validate_iap_jwt" {
  value       = var.validate_iap_jwt
  description = "Whether API validates IAP JWT assertions"
}

output "google_directory_enabled" {
  value       = var.google_directory_enabled
  description = "Whether Google Directory lookup is enabled"
}

output "google_directory_admin_user_email" {
  value       = var.google_directory_admin_user_email
  description = "Delegated admin user email for Google Directory lookups"
}

output "google_directory_scope" {
  value       = var.google_directory_scope
  description = "Google Directory OAuth scope"
}

output "google_directory_org_unit_key_prefix" {
  value       = var.google_directory_org_unit_key_prefix
  description = "Group-key prefix for org unit derived keys"
}

output "google_directory_include_org_unit_hierarchy" {
  value       = var.google_directory_include_org_unit_hierarchy
  description = "Whether org unit hierarchy keys are emitted"
}

output "google_directory_custom_attribute_key_prefix" {
  value       = var.google_directory_custom_attribute_key_prefix
  description = "Group-key prefix for custom schema derived keys"
}

output "google_directory_credentials_secret_name" {
  value       = local.google_directory_credentials_secret_name
  description = "Secret Manager secret ID containing Google Directory credentials JSON"
}
