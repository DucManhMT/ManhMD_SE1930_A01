using Xunit;

// Disable parallel test execution so integration tests targeting local SQL Server database do not interfere with each other
[assembly: CollectionBehavior(DisableTestParallelization = true)]
