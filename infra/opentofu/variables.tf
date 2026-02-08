variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "region" {
  description = "GCP region"
  type        = string
}

variable "zone" {
  description = "GCP zone"
  type        = string
}

variable "name_prefix" {
  description = "Prefix used for resource names"
  type        = string
  default     = "freetool"
}

variable "domain_name" {
  description = "Public DNS name used for HTTPS (e.g. freetool.example.com)"
  type        = string
}

variable "dns_managed_zone" {
  description = "Optional Cloud DNS managed zone name for creating an A record"
  type        = string
  default     = ""
}

variable "oauth2_client_id" {
  description = "OAuth client ID used by IAP"
  type        = string
  default     = ""

  validation {
    condition     = trimspace(var.oauth2_client_id) != ""
    error_message = "Set oauth2_client_id."
  }
}

variable "oauth2_client_secret" {
  description = "OAuth client secret used by IAP"
  type        = string
  sensitive   = true
  default     = ""

  validation {
    condition     = trimspace(var.oauth2_client_secret) != ""
    error_message = "Set oauth2_client_secret."
  }
}

variable "machine_type" {
  description = "GCE machine type"
  type        = string
  default     = "e2-micro"
}

variable "boot_disk_size_gb" {
  description = "Boot disk size in GB for the VM"
  type        = number
  default     = 10
}

variable "artifact_registry_repo" {
  description = "Artifact Registry Docker repository name"
  type        = string
  default     = "freetool"
}

variable "artifact_registry_location" {
  description = "Artifact Registry location"
  type        = string
  default     = "us-central1"
}

variable "image_name" {
  description = "Container image name within Artifact Registry"
  type        = string
  default     = "freetool-api"
}

variable "initial_image_tag" {
  description = "Initial image tag used on first boot"
  type        = string
  default     = "latest"
}

variable "org_admin_email" {
  description = "Optional Freetool org admin email"
  type        = string
  default     = ""
}

variable "validate_iap_jwt" {
  description = "Whether API should validate IAP JWT assertions"
  type        = bool
  default     = true
}

variable "iap_jwt_audience" {
  description = "IAP JWT audience passed to the API (Auth:IAP:JwtAudience)"
  type        = string
  default     = ""
}

variable "allow_ssh_from" {
  description = "CIDRs allowed to SSH directly to VM"
  type        = list(string)
  default     = ["35.235.240.0/20"]
}

variable "data_disk_size_gb" {
  description = "Persistent data disk size in GB for Freetool and OpenFGA state"
  type        = number
  default     = 30
}

variable "data_disk_type" {
  description = "Persistent disk type for app data"
  type        = string
  default     = "pd-balanced"
}

variable "data_mount_path" {
  description = "Mount path used for the persistent data disk"
  type        = string
  default     = "/mnt/freetool-data"
}

variable "preserve_data_disk_on_destroy" {
  description = "Prevent accidental deletion of persistent data disk on tofu destroy"
  type        = bool
  default     = true
}

variable "artifact_cleanup_policy_dry_run" {
  description = "Whether Artifact Registry cleanup policy runs in dry-run mode"
  type        = bool
  default     = false
}

variable "artifact_keep_recent_count" {
  description = "How many recent versions to keep for the main app image package"
  type        = number
  default     = 5
}

variable "artifact_delete_older_than" {
  description = "Delete threshold for old image versions (e.g. 1d, 7d, 30d)"
  type        = string
  default     = "7d"
}
