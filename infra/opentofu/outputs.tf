output "vm_name" {
  value       = google_compute_instance.freetool.name
  description = "Compute instance name"
}

output "vm_zone" {
  value       = google_compute_instance.freetool.zone
  description = "Compute instance zone"
}

output "vm_external_ip" {
  value       = google_compute_address.vm.address
  description = "VM external IP"
}

output "load_balancer_ip" {
  value       = google_compute_global_address.freetool.address
  description = "Global IP for HTTPS load balancer"
}

output "https_url" {
  value       = "https://${var.domain_name}/freetool/"
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
