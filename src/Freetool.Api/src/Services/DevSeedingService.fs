namespace Freetool.Api.Services

open System.Threading.Tasks
open Freetool.Domain
open Freetool.Domain.ValueObjects
open Freetool.Domain.Entities
open Freetool.Application.Interfaces

module DevSeedingService =

    /// Ensures OpenFGA relationships exist for dev mode users and spaces.
    /// This is called after the authorization model is written to handle the case
    /// where the OpenFGA store was recreated but the database still has users.
    /// All relationship creation is idempotent - if the relationships already exist, this succeeds.
    let ensureOpenFgaRelationshipsAsync
        (userRepository: IUserRepository)
        (spaceRepository: ISpaceRepository)
        (authService: IAuthorizationService)
        : Task<unit> =
        task {
            eprintfn "[DEV MODE] Ensuring OpenFGA relationships exist..."

            // Parse emails for lookup
            let adminEmailResult = Email.Create(Some "admin@test.local")
            let moderatorEmailResult = Email.Create(Some "moderator@test.local")
            let memberEmailResult = Email.Create(Some "member@test.local")
            let nopermEmailResult = Email.Create(Some "noperm@test.local")

            match adminEmailResult, moderatorEmailResult, memberEmailResult, nopermEmailResult with
            | Ok adminEmail, Ok moderatorEmail, Ok memberEmail, Ok nopermEmail ->
                // Look up users by email
                let! adminUserOpt = userRepository.GetByEmailAsync adminEmail
                let! moderatorUserOpt = userRepository.GetByEmailAsync moderatorEmail
                let! memberUserOpt = userRepository.GetByEmailAsync memberEmail
                let! nopermUserOpt = userRepository.GetByEmailAsync nopermEmail

                match adminUserOpt with
                | None -> eprintfn "[DEV MODE] Admin user not found, skipping relationship seeding"
                | Some adminUser ->
                    let adminUserId = adminUser.State.Id
                    let adminUserIdStr = adminUserId.Value.ToString()

                    // 1. Ensure org admin relationship
                    try
                        do! authService.InitializeOrganizationAsync "default" adminUserIdStr
                        eprintfn "[DEV MODE] Ensured org admin relationship for user %s" adminUserIdStr
                    with ex ->
                        eprintfn "[DEV MODE] Warning: Failed to ensure org admin relationship: %s" ex.Message

                    // 2. Look up the test space by name
                    let! spaces = spaceRepository.GetAllAsync 0 100

                    let testSpaceOpt = spaces |> List.tryFind (fun s -> s.State.Name = "Test Space")

                    match testSpaceOpt with
                    | None -> eprintfn "[DEV MODE] Test Space not found, skipping space relationship seeding"
                    | Some space ->
                        let spaceId = Space.getId space
                        let spaceIdStr = spaceId.Value.ToString()

                        // 3. Ensure organization relation for the space
                        try
                            let orgTuple =
                                { Subject = Organization "default"
                                  Relation = SpaceOrganization
                                  Object = SpaceObject spaceIdStr }

                            do! authService.CreateRelationshipsAsync [ orgTuple ]
                            eprintfn "[DEV MODE] Ensured organization relation for space %s" spaceIdStr
                        with ex ->
                            eprintfn "[DEV MODE] Warning: Failed to ensure org relation for space: %s" ex.Message

                        // 4. Ensure moderator relation
                        match moderatorUserOpt with
                        | None -> eprintfn "[DEV MODE] Moderator user not found, skipping moderator relation"
                        | Some moderatorUser ->
                            let moderatorUserId = moderatorUser.State.Id
                            let moderatorUserIdStr = moderatorUserId.Value.ToString()

                            try
                                let moderatorTuple =
                                    { Subject = User moderatorUserIdStr
                                      Relation = SpaceModerator
                                      Object = SpaceObject spaceIdStr }

                                do! authService.CreateRelationshipsAsync [ moderatorTuple ]
                                eprintfn "[DEV MODE] Ensured moderator relation for user %s" moderatorUserIdStr
                            with ex ->
                                eprintfn "[DEV MODE] Warning: Failed to ensure moderator relation: %s" ex.Message

                        // 5. Ensure member relations
                        let memberTuples = ResizeArray<RelationshipTuple>()

                        match memberUserOpt with
                        | Some memberUser ->
                            let memberUserId = memberUser.State.Id
                            let memberUserIdStr = memberUserId.Value.ToString()

                            memberTuples.Add(
                                { Subject = User memberUserIdStr
                                  Relation = SpaceMember
                                  Object = SpaceObject spaceIdStr }
                            )

                            // Also add run_app permission for member
                            try
                                let runAppTuple =
                                    { Subject = User memberUserIdStr
                                      Relation = AppRun
                                      Object = SpaceObject spaceIdStr }

                                do! authService.CreateRelationshipsAsync [ runAppTuple ]
                                eprintfn "[DEV MODE] Ensured run_app permission for member user %s" memberUserIdStr
                            with ex ->
                                eprintfn "[DEV MODE] Warning: Failed to ensure run_app permission: %s" ex.Message
                        | None -> eprintfn "[DEV MODE] Member user not found"

                        match nopermUserOpt with
                        | Some nopermUser ->
                            let nopermUserId = nopermUser.State.Id
                            let nopermUserIdStr = nopermUserId.Value.ToString()

                            memberTuples.Add(
                                { Subject = User nopermUserIdStr
                                  Relation = SpaceMember
                                  Object = SpaceObject spaceIdStr }
                            )
                        | None -> eprintfn "[DEV MODE] Noperm user not found"

                        if memberTuples.Count > 0 then
                            try
                                do! authService.CreateRelationshipsAsync(memberTuples |> Seq.toList)
                                eprintfn "[DEV MODE] Ensured member relations"
                            with ex ->
                                eprintfn "[DEV MODE] Warning: Failed to ensure member relations: %s" ex.Message

                        eprintfn "[DEV MODE] OpenFGA relationship seeding complete!"
            | _ -> eprintfn "[DEV MODE] Failed to parse dev user emails"
        }

    /// Seeds the dev database with test users, a space, resource, folder, and app
    /// Only runs when the database is empty (no users exist)
    let seedDataAsync
        (userRepository: IUserRepository)
        (spaceRepository: ISpaceRepository)
        (resourceRepository: IResourceRepository)
        (folderRepository: IFolderRepository)
        (appRepository: IAppRepository)
        (authService: IAuthorizationService)
        : Task<unit> =
        task {
            // Check if database already has users - if so, skip seeding
            let! userCount = userRepository.GetCountAsync()

            if userCount > 0 then
                eprintfn "[DEV MODE] Database already has users, skipping seed data"
                return ()
            else
                eprintfn "[DEV MODE] Seeding dev database with test data..."

                // Create test users
                // 1. Admin user - will become org admin
                let adminEmail =
                    match Email.Create(Some "admin@test.local") with
                    | Ok e -> e
                    | Error _ -> failwith "Invalid admin email"

                let adminUser = User.create "Org Admin" adminEmail None

                match! userRepository.AddAsync adminUser with
                | Error err -> eprintfn "[DEV MODE] Failed to create admin user: %A" err
                | Ok savedAdminUser ->
                    let adminUserId = savedAdminUser.State.Id
                    eprintfn "[DEV MODE] Created admin user: %s" (adminUserId.Value.ToString())

                    // Set admin as org admin
                    try
                        do! authService.InitializeOrganizationAsync "default" (adminUserId.Value.ToString())
                        eprintfn "[DEV MODE] Set admin user as organization admin"
                    with ex ->
                        eprintfn "[DEV MODE] Warning: Failed to set org admin: %s" ex.Message

                    // 2. Moderator user
                    let moderatorEmail =
                        match Email.Create(Some "moderator@test.local") with
                        | Ok e -> e
                        | Error _ -> failwith "Invalid moderator email"

                    let moderatorUser = User.create "Space Moderator" moderatorEmail None

                    match! userRepository.AddAsync moderatorUser with
                    | Error err -> eprintfn "[DEV MODE] Failed to create moderator user: %A" err
                    | Ok savedModeratorUser ->
                        let moderatorUserId = savedModeratorUser.State.Id
                        eprintfn "[DEV MODE] Created moderator user: %s" (moderatorUserId.Value.ToString())

                        // 3. Member user
                        let memberEmail =
                            match Email.Create(Some "member@test.local") with
                            | Ok e -> e
                            | Error _ -> failwith "Invalid member email"

                        let memberUser = User.create "Regular Member" memberEmail None

                        match! userRepository.AddAsync memberUser with
                        | Error err -> eprintfn "[DEV MODE] Failed to create member user: %A" err
                        | Ok savedMemberUser ->
                            let memberUserId = savedMemberUser.State.Id
                            eprintfn "[DEV MODE] Created member user: %s" (memberUserId.Value.ToString())

                            // 4. No permissions user
                            let nopermEmail =
                                match Email.Create(Some "noperm@test.local") with
                                | Ok e -> e
                                | Error _ -> failwith "Invalid noperm email"

                            let nopermUser = User.create "No Permissions" nopermEmail None

                            match! userRepository.AddAsync nopermUser with
                            | Error err -> eprintfn "[DEV MODE] Failed to create noperm user: %A" err
                            | Ok savedNopermUser ->
                                let nopermUserId = savedNopermUser.State.Id
                                eprintfn "[DEV MODE] Created noperm user: %s" (nopermUserId.Value.ToString())

                                // 5. Not a member user - exists but is not a member of any space
                                let notamemberEmail =
                                    match Email.Create(Some "notamember@test.local") with
                                    | Ok e -> e
                                    | Error _ -> failwith "Invalid notamember email"

                                let notamemberUser = User.create "Not a Member" notamemberEmail None

                                match! userRepository.AddAsync notamemberUser with
                                | Error err -> eprintfn "[DEV MODE] Failed to create notamember user: %A" err
                                | Ok savedNotamemberUser ->
                                    let notamemberUserId = savedNotamemberUser.State.Id

                                    eprintfn
                                        "[DEV MODE] Created notamember user: %s"
                                        (notamemberUserId.Value.ToString())

                                    // Suppress unused variable warning - this user intentionally has no space membership
                                    ignore notamemberUserId

                                // Create test space with moderator and members
                                match
                                    Space.create
                                        adminUserId
                                        "Test Space"
                                        moderatorUserId
                                        (Some [ memberUserId; nopermUserId ])
                                with
                                | Error err -> eprintfn "[DEV MODE] Failed to create space: %A" err
                                | Ok space ->
                                    match! spaceRepository.AddAsync space with
                                    | Error err -> eprintfn "[DEV MODE] Failed to save space: %A" err
                                    | Ok() ->
                                        let spaceId = Space.getId space
                                        let spaceIdStr = spaceId.Value.ToString()
                                        eprintfn "[DEV MODE] Created Test Space: %s" spaceIdStr

                                        // Set up organization relation for the space
                                        try
                                            let orgTuple =
                                                { Subject = Organization "default"
                                                  Relation = SpaceOrganization
                                                  Object = SpaceObject spaceIdStr }

                                            do! authService.CreateRelationshipsAsync [ orgTuple ]
                                            eprintfn "[DEV MODE] Set up organization relation for space"
                                        with ex ->
                                            eprintfn
                                                "[DEV MODE] Warning: Failed to set org relation for space: %s"
                                                ex.Message

                                        // Set up moderator relation in OpenFGA
                                        try
                                            let moderatorTuple =
                                                { Subject = User(moderatorUserId.Value.ToString())
                                                  Relation = SpaceModerator
                                                  Object = SpaceObject spaceIdStr }

                                            do! authService.CreateRelationshipsAsync [ moderatorTuple ]
                                            eprintfn "[DEV MODE] Set up moderator relation"
                                        with ex ->
                                            eprintfn
                                                "[DEV MODE] Warning: Failed to set moderator relation: %s"
                                                ex.Message

                                        // Set up member relations in OpenFGA
                                        try
                                            let memberTuples =
                                                [ { Subject = User(memberUserId.Value.ToString())
                                                    Relation = SpaceMember
                                                    Object = SpaceObject spaceIdStr }
                                                  { Subject = User(nopermUserId.Value.ToString())
                                                    Relation = SpaceMember
                                                    Object = SpaceObject spaceIdStr } ]

                                            do! authService.CreateRelationshipsAsync memberTuples
                                            eprintfn "[DEV MODE] Set up member relations"
                                        with ex ->
                                            eprintfn "[DEV MODE] Warning: Failed to set member relations: %s" ex.Message

                                        // Give member user run_app permission
                                        try
                                            let runAppTuple =
                                                { Subject = User(memberUserId.Value.ToString())
                                                  Relation = AppRun
                                                  Object = SpaceObject spaceIdStr }

                                            do! authService.CreateRelationshipsAsync [ runAppTuple ]
                                            eprintfn "[DEV MODE] Set up run_app permission for member"
                                        with ex ->
                                            eprintfn
                                                "[DEV MODE] Warning: Failed to set run_app permission: %s"
                                                ex.Message

                                        // Create a resource in the space
                                        match
                                            Resource.create
                                                adminUserId
                                                spaceId
                                                "Sample API"
                                                "A sample API resource for testing"
                                                "https://httpbin.org/get"
                                                []
                                                []
                                                []
                                        with
                                        | Error err -> eprintfn "[DEV MODE] Failed to create resource: %A" err
                                        | Ok resource ->
                                            match! resourceRepository.AddAsync resource with
                                            | Error err -> eprintfn "[DEV MODE] Failed to save resource: %A" err
                                            | Ok() ->
                                                let resourceId = Resource.getId resource

                                                eprintfn
                                                    "[DEV MODE] Created Sample API resource: %s"
                                                    (resourceId.Value.ToString())

                                                // Create a folder in the space
                                                match Folder.create adminUserId "Sample Folder" None spaceId with
                                                | Error err -> eprintfn "[DEV MODE] Failed to create folder: %A" err
                                                | Ok folder ->
                                                    match! folderRepository.AddAsync folder with
                                                    | Error err -> eprintfn "[DEV MODE] Failed to save folder: %A" err
                                                    | Ok() ->
                                                        let folderId = Folder.getId folder

                                                        eprintfn
                                                            "[DEV MODE] Created Sample Folder: %s"
                                                            (folderId.Value.ToString())

                                                        // Create an app in the folder
                                                        match
                                                            App.createWithResource
                                                                adminUserId
                                                                "Hello World"
                                                                folderId
                                                                resource
                                                                HttpMethod.Get
                                                                []
                                                                None
                                                                []
                                                                []
                                                                []
                                                        with
                                                        | Error err ->
                                                            eprintfn "[DEV MODE] Failed to create app: %A" err
                                                        | Ok app ->
                                                            match! appRepository.AddAsync app with
                                                            | Error err ->
                                                                eprintfn "[DEV MODE] Failed to save app: %A" err
                                                            | Ok() ->
                                                                let appId = App.getId app

                                                                eprintfn
                                                                    "[DEV MODE] Created Hello World app: %s"
                                                                    (appId.Value.ToString())

                                                                eprintfn "[DEV MODE] Dev database seeding complete!"

                return ()
        }
