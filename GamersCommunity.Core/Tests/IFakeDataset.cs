using Microsoft.EntityFrameworkCore;

namespace GamersCommunity.Core.Tests
{
    /// <summary>
    /// Defines a fake dataset builder used to initialize
    /// an in-memory test database with base data.
    /// </summary>
    public interface IFakeDataset<TContext> where TContext : DbContext
    {
        /// <summary>
        /// Creates and returns a fully seeded in-memory database context.
        /// </summary>
        TContext CreateFakeContext();
    }
}
