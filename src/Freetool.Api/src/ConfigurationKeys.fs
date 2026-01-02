namespace Freetool.Api

/// Centralized configuration keys to avoid magic strings throughout the codebase.
/// Using [<Literal>] enables compile-time string substitution and IDE autocomplete.
module ConfigurationKeys =

    /// Connection string key for the default database connection
    [<Literal>]
    let DefaultConnection = "DefaultConnection"

    /// OpenFGA authorization service configuration keys
    module OpenFGA =
        /// The API URL for the OpenFGA service
        [<Literal>]
        let ApiUrl = "OpenFGA:ApiUrl"

        /// The store ID for the OpenFGA authorization store
        [<Literal>]
        let StoreId = "OpenFGA:StoreId"

        /// Email address of the organization admin (for automatic admin setup)
        [<Literal>]
        let OrgAdminEmail = "OpenFGA:OrgAdminEmail"

    /// Environment variable keys
    module Environment =
        /// OpenTelemetry Protocol (OTLP) exporter endpoint
        [<Literal>]
        let OtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT"

        /// Development mode flag (set to "true" to enable dev features)
        [<Literal>]
        let DevMode = "FREETOOL_DEV_MODE"
