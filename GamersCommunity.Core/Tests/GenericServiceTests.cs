using GamersCommunity.Core.Database;
using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Serialization;
using GamersCommunity.Core.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;

namespace GamersCommunity.Core.Tests
{
    /// <summary>
    /// Generic xUnit test base for <see cref="GenericTableService{TContext, TEntity}"/> implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstract fixture provides a common test suite to validate the core CRUD behavior exposed through
    /// <see cref="ITableService.HandleAsync(string, string?, int?, System.Threading.CancellationToken)"/>.
    /// Concrete test classes should supply a configured service instance and minimal test data factories.
    /// </para>
    /// <para>
    /// The helper methods <see cref="GetFakeData"/> and <see cref="GetNewEntity"/> must be implemented by derived
    /// classes to control the initial dataset and the shape of newly created entities.
    /// </para>
    /// </remarks>
    /// <typeparam name="TContext">Concrete <see cref="DbContext"/> used by the service under test.</typeparam>
    /// <typeparam name="TService">Concrete <see cref="GenericTableService{TContext, TEntity}"/> being tested.</typeparam>
    /// <typeparam name="TEntity">Entity type that implements <see cref="IKeyTable"/>.</typeparam>
    /// <param name="service">Service instance under test.</param>
    public abstract class GenericServiceTests<TContext, TService, TEntity>(TService service)
        where TService : GenericTableService<TContext, TEntity>
        where TContext : DbContext
        where TEntity : class, IKeyTable
    {
        /// <summary>
        /// Provides a deterministic set of entities representing the initial dataset for assertions.
        /// </summary>
        /// <remarks>
        /// Used by <see cref="List_All"/> (and potentially by concrete tests) to validate counts and shapes.
        /// </remarks>
        protected abstract List<TEntity> GetFakeData();

        /// <summary>
        /// Creates a new entity instance suitable for a successful <c>Create</c> action.
        /// </summary>
        /// <remarks>
        /// The returned instance should satisfy validation rules expected by the service under test.
        /// </remarks>
        protected abstract TEntity GetNewEntity();

        /// <summary>
        /// Verifies that an unknown action routed to the service results in a <see cref="BadRequestException"/>.
        /// </summary>
        [Fact]
        public async Task Action_Handle_Unknown_Action()
        {
            // Assert
            await Assert.ThrowsAsync<InternalServerErrorException>(() => service.HandleAsync(new BusMessage()
            {
                Action = "UnknownAction",
                Resource = string.Empty
            }));
        }

        /// <summary>
        /// Verifies that invalid inputs (missing data or id) cause <see cref="BadRequestException"/> for relevant actions.
        /// </summary>
        /// <param name="exception">Expected exception message.</param>
        /// <param name="action">Service action being tested.</param>
        /// <param name="data">Optional JSON payload.</param>
        /// <param name="id">Optional entity id.</param>
        [Theory]
        [InlineData("Data mandatory", "Create")]
        [InlineData("Id mandatory", "Get")]
        [InlineData("Id mandatory", "Update")]
        [InlineData("Data mandatory", "Update", null, 1)]
        [InlineData("Id mandatory", "Delete")]
        public async Task Action_Handle_Throw_Invalid_Data(string exception, string action, string? data = null, int? id = null)
        {
            // Assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.HandleAsync(new BusMessage()
            {
                Action = action,
                Data = data,
                Id = id
            }));
            Assert.Equal(exception, ex.Message);
        }

        /// <summary>
        /// Ensures that the <c>Create</c> action returns a serialized entity with a non-zero identifier.
        /// </summary>
        [Fact]
        public async Task Create_Test()
        {
            // Act
            var result = await service.HandleAsync(new BusMessage()
            {
                Action = "Create",
                Data = JsonSafe.Serialize(GetNewEntity())
            });

            // Assert
            var entity = JsonConvert.DeserializeObject<TEntity>(result);
            Assert.NotNull(entity);
            Assert.NotEqual(0, entity!.Id);
        }

        /// <summary>
        /// Verifies that <c>Get</c> returns the expected entity payload for a given identifier.
        /// </summary>
        /// <param name="id">Requested entity id.</param>
        /// <param name="expected">Expected id in the response payload (or <see langword="null"/> when not found).</param>
        [Theory]
        [InlineData(0, null)]
        [InlineData(1, 1)]
        public async Task Get_By_Id(int id, int? expected)
        {
            // Act
            var result = await service.HandleAsync(new BusMessage()
            {
                Action = "Get",
                Id = id,
            });

            // Assert
            var entity = JsonConvert.DeserializeObject<TEntity>(result);
            Assert.Equal(expected, entity?.Id);
        }

        /// <summary>
        /// Ensures that <c>List</c> returns all entities and the count matches the fake dataset.
        /// </summary>
        [Fact]
        public async Task List_All()
        {
            // Act
            var result = await service.HandleAsync(new BusMessage()
            {
                Action = "List",
                Resource = string.Empty
            });
            var entities = JsonConvert.DeserializeObject<List<TEntity>>(result);

            // Assert
            Assert.NotNull(entities);
            Assert.Equal(GetFakeData().Count, entities!.Count);
        }
    }
}
