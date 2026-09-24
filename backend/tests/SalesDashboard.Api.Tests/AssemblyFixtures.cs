using Xunit;

// One PostgreSQL container for the whole test run (assembly-level fixture, xUnit v3).
[assembly: AssemblyFixture(typeof(SalesDashboard.Api.Tests.PostgresFixture))]
