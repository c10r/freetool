namespace Freetool.Api.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Freetool.Application.Interfaces

/// Controller for OpenFGA initialization operations
[<ApiController>]
[<Route("admin/openfga")>]
type OpenFgaInitController(authService: IAuthorizationService) =
    inherit ControllerBase()

    /// Writes the authorization model to the configured OpenFGA store
    /// Call this endpoint once after setting up your StoreId in configuration
    [<HttpPost("write-model")>]
    [<ProducesResponseType(typeof<
                               {| authorizationModelId: string
                                  message: string |}
                            >,
                           StatusCodes.Status200OK)>]
    [<ProducesResponseType(StatusCodes.Status500InternalServerError)>]
    member this.WriteAuthorizationModel() : Task<IActionResult> =
        task {
            try
                // Write the authorization model
                let! modelResponse = authService.WriteAuthorizationModelAsync()

                return
                    this.Ok(
                        {| authorizationModelId = modelResponse.AuthorizationModelId
                           message = "Authorization model written successfully. OpenFGA is now ready to use." |}
                    )
                    :> IActionResult
            with ex ->
                return
                    this.StatusCode(
                        500,
                        {| error = "Failed to write authorization model"
                           message = ex.Message
                           details =
                            "Make sure you have configured the StoreId in your application settings and that OpenFGA is running." |}
                    )
                    :> IActionResult
        }
