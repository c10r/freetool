namespace Freetool.Infrastructure.Tests.Database

open System
open System.IO
open Xunit
open Microsoft.Data.Sqlite
open Freetool.Infrastructure.Database

module PersistenceTests =

    [<Fact>]
    let ``getDatabaseConnectionString creates valid SQLite connection string`` () =
        let dataSource = "test.db"
        let connectionString = Persistence.getDatabaseConnectionString dataSource

        Assert.Contains("Data Source=test.db", connectionString)
        Assert.True(connectionString.Length > 0)

    [<Fact>]
    let ``getDatabaseConnectionString handles paths with spaces`` () =
        let dataSource = "test database with spaces.db"
        let connectionString = Persistence.getDatabaseConnectionString dataSource

        Assert.Contains("test database with spaces.db", connectionString)

    [<Fact>]
    let ``getDatabaseConnectionString handles relative paths`` () =
        let dataSource = "./data/test.db"
        let connectionString = Persistence.getDatabaseConnectionString dataSource

        Assert.Contains("./data/test.db", connectionString)

    [<Fact>]
    let ``getDatabaseConnectionString handles absolute paths`` () =
        let dataSource = "/tmp/test.db"
        let connectionString = Persistence.getDatabaseConnectionString dataSource

        Assert.Contains("/tmp/test.db", connectionString)

    [<Fact>]
    let ``getDbConnectionAsync returns valid SQLite connection`` () = async {
        let connectionString = Persistence.getDatabaseConnectionString ":memory:"
        let! connection = Persistence.getDbConnectionAsync connectionString

        Assert.NotNull(connection)
        Assert.IsType<SqliteConnection>(connection) |> ignore
        Assert.Equal(connectionString, connection.ConnectionString)
    }

    [<Fact>]
    let ``getDatabaseConnection returns valid SQLite connection`` () =
        let connectionString = Persistence.getDatabaseConnectionString ":memory:"
        let connection = Persistence.getDatabaseConnection connectionString

        Assert.NotNull(connection)
        Assert.IsType<SqliteConnection>(connection) |> ignore
        Assert.Equal(connectionString, connection.ConnectionString)

    [<Fact>]
    let ``withDbIoAsync opens connection and executes callback`` () = async {
        let connectionString = Persistence.getDatabaseConnectionString ":memory:"

        let! result =
            Persistence.withDbIoAsync connectionString (fun connection -> async {
                Assert.NotNull(connection)
                Assert.IsType<SqliteConnection>(connection) |> ignore
                return "test result"
            })

        Assert.Equal("test result", result)
    }

    [<Fact>]
    let ``withDbIoAsync can execute SQL commands`` () = async {
        let connectionString = Persistence.getDatabaseConnectionString ":memory:"

        let! result =
            Persistence.withDbIoAsync connectionString (fun connection -> async {
                use command = connection.CreateCommand()
                command.CommandText <- "SELECT 1 as TestValue"
                let! reader = command.ExecuteReaderAsync() |> Async.AwaitTask

                Assert.True(reader.Read())
                let value = reader.GetInt32(0)
                return value
            })

        Assert.Equal(1, result)
    }

    [<Fact>]
    let ``withDbIoAsync properly disposes connection`` () = async {
        let connectionString = Persistence.getDatabaseConnectionString ":memory:"
        let mutable connectionState = ""

        let! _ =
            Persistence.withDbIoAsync connectionString (fun connection -> async {
                connectionState <- connection.State.ToString()
                return ()
            })

        Assert.Equal("Open", connectionState)
    }

    [<Fact>]
    let ``upgradeDatabase succeeds with valid connection string`` () =
        let tempFile = Path.GetTempFileName()

        try
            let connectionString = Persistence.getDatabaseConnectionString tempFile

            // This should not throw an exception
            Persistence.upgradeDatabase connectionString

            // Verify the database file was created
            Assert.True(File.Exists(tempFile))
        finally
            if File.Exists(tempFile) then
                File.Delete(tempFile)

    [<Fact>]
    let ``upgradeDatabase creates database file if it doesn't exist`` () =
        let tempDir = Path.GetTempPath()
        let dbFile = Path.Combine(tempDir, $"test_{Guid.NewGuid()}.db")

        try
            let connectionString = Persistence.getDatabaseConnectionString dbFile

            Assert.False(File.Exists(dbFile))

            Persistence.upgradeDatabase connectionString

            Assert.True(File.Exists(dbFile))
        finally
            if File.Exists(dbFile) then
                File.Delete(dbFile)

    [<Fact>]
    let ``upgradeDatabase works with in-memory database`` () =
        let connectionString = Persistence.getDatabaseConnectionString ":memory:"

        // This should not throw an exception
        Persistence.upgradeDatabase connectionString

    [<Fact>]
    let ``upgradeDatabase can be run multiple times safely`` () =
        let tempFile = Path.GetTempFileName()

        try
            let connectionString = Persistence.getDatabaseConnectionString tempFile

            // Run upgrade twice
            Persistence.upgradeDatabase connectionString
            Persistence.upgradeDatabase connectionString

            Assert.True(File.Exists(tempFile))
        finally
            if File.Exists(tempFile) then
                File.Delete(tempFile)

    [<Fact>]
    let ``connection string variations work correctly`` () =
        let testCases = [ "test.db"; ":memory:"; "./data/test.db"; "C:\\temp\\test.db"; "/tmp/test.db" ]

        for dataSource in testCases do
            let connectionString = Persistence.getDatabaseConnectionString dataSource
            let connection = Persistence.getDatabaseConnection connectionString

            Assert.NotNull(connection)
            Assert.IsType<SqliteConnection>(connection) |> ignore

    [<Fact>]
    let ``getDbConnectionAsync and getDatabaseConnection return equivalent connections`` () = async {
        let connectionString = Persistence.getDatabaseConnectionString ":memory:"

        let syncConnection = Persistence.getDatabaseConnection connectionString
        let! asyncConnection = Persistence.getDbConnectionAsync connectionString

        Assert.Equal(syncConnection.ConnectionString, asyncConnection.ConnectionString)
        Assert.Equal(syncConnection.GetType(), asyncConnection.GetType())
    }

    [<Fact>]
    let ``withDbIoAsync handles callback exceptions gracefully`` () = async {
        let connectionString = Persistence.getDatabaseConnectionString ":memory:"

        try
            let! _ = Persistence.withDbIoAsync connectionString (fun _ -> async { failwith "Test exception" })
            Assert.True(false, "Should have thrown exception")
        with ex ->
            Assert.Equal("Test exception", ex.Message)
    }

    [<Fact>]
    let ``connection string contains expected SQLite parameters`` () =
        let dataSource = "test.db"
        let connectionString = Persistence.getDatabaseConnectionString dataSource

        // Parse the connection string to verify it's valid
        let builder = SqliteConnectionStringBuilder(connectionString)
        Assert.Equal(dataSource, builder.DataSource)