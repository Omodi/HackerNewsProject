using HackerNewsApi.UnitTests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace HackerNewsApi.UnitTests.Infrastructure;

public class FtsStructureDebugTest : DatabaseTestBase
{
    private readonly ITestOutputHelper _output;

    public FtsStructureDebugTest(DatabaseTestFixture databaseFixture, ITestOutputHelper output) 
        : base(databaseFixture)
    {
        _output = output;
    }

    [Fact]
    public async Task DebugFtsStructure_ShouldShowActualTableStructure()
    {
        // Get the database connection
        var connection = Context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            // Check if StoriesSearch table exists
            using var checkTableCmd = connection.CreateCommand();
            checkTableCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='StoriesSearch';";
            var tableName = await checkTableCmd.ExecuteScalarAsync();
            _output.WriteLine($"StoriesSearch table exists: {tableName != null}");

            if (tableName != null)
            {
                // Get table structure
                using var structureCmd = connection.CreateCommand();
                structureCmd.CommandText = "PRAGMA table_info(StoriesSearch);";
                using var reader = await structureCmd.ExecuteReaderAsync();
                
                _output.WriteLine("StoriesSearch table structure:");
                while (await reader.ReadAsync())
                {
                    var cid = reader.GetInt32(0);      // cid
                    var name = reader.GetString(1);    // name
                    var type = reader.GetString(2);    // type
                    var notNull = reader.GetInt32(3);  // notnull
                    var defaultValue = reader.IsDBNull(4) ? "NULL" : reader.GetString(4); // dflt_value
                    var pk = reader.GetInt32(5);       // pk
                    
                    _output.WriteLine($"  Column {cid}: {name} ({type}) NotNull:{notNull} Default:{defaultValue} PK:{pk}");
                }

                // Get the actual CREATE statement
                using var createCmd = connection.CreateCommand();
                createCmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='StoriesSearch';";
                var createSql = await createCmd.ExecuteScalarAsync();
                _output.WriteLine($"CREATE statement: {createSql}");

                // Test if we can query enhanced columns
                try
                {
                    using var testCmd = connection.CreateCommand();
                    testCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('StoriesSearch') WHERE name IN ('HasUrl', 'Score', 'CreatedAt', 'CommentCount');";
                    var enhancedColumns = await testCmd.ExecuteScalarAsync();
                    _output.WriteLine($"Enhanced columns found: {enhancedColumns}/4");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Error checking enhanced columns: {ex.Message}");
                }
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}