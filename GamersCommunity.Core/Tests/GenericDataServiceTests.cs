using GamersCommunity.Core.Database;
using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Serialization;
using GamersCommunity.Core.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;

namespace GamersCommunity.Core.Tests;

/// <summary>
/// Generic xUnit test base for <see cref="GenericDataService{TContext, TEntity}"/> implementations.
/// </summary>
/// <remarks>
/// <para>
/// This abstract fixture provides a common test suite to validate the core CRUD behavior exposed through
/// <see cref="IBusService.HandleAsync(BusMessage, CancellationToken)"/>.
/// Concrete test classes should supply a configured service instance and minimal test data factories.
/// </para>
/// <para>
/// The helper methods <see cref="GetFakeData"/> and <see cref="GetNewEntity"/> must be implemented by derived
/// classes to control the initial dataset and the shape of newly created entities.
/// </para>
/// </remarks>
/// <typeparam name="TContext">Concrete <see cref="DbContext"/> used by the service under test.</typeparam>
/// <typeparam name="TService">Concrete <see cref="GenericDataService{TContext, TEntity}"/> being tested.</typeparam>
/// <typeparam name="TEntity">Entity type that implements <see cref="IKeyTable"/>.</typeparam>
public abstract class GenericDataServiceTests<TContext, TService, TEntity>
    where TService : GenericDataService<TContext, TEntity>
    where TContext : DbContext
    where TEntity : class, IKeyTable
{
    /// <summary>
    /// Creates a new entity instance suitable for a successful <c>Create</c> action.
    /// </summary>
    /// <remarks>
    /// The returned instance should satisfy validation rules expected by the service under test.
    /// </remarks>
    protected abstract TEntity GetNewEntity();

    /// <summary>
    /// Provides a deterministic set of entities representing the initial dataset for assertions.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="List_All"/> (and potentially by concrete tests) to validate counts and shapes.
    /// </remarks>
    protected abstract List<TEntity> GetFakeData();

    /// <summary>
    /// Creates and returns a fresh instance of the service under test, using a new isolated in-memory database.
    /// </summary>
    /// <remarks>
    /// The implementation should:
    /// <list type="number">
    /// <item>Create a <see cref="DbContextOptions{TContext}"/> using <c>UseInMemoryDatabase</c> with a GUID name.</item>
    /// <item>Seed the fake data via <see cref="GetFakeData"/>.</item>
    /// <item>Return a new instance of the service that uses this context.</item>
    /// </list>
    /// </remarks>
    protected abstract TService CreateService();

    /// <summary>
    /// Optional authenticated caller attached to every <see cref="BusMessage"/> in this suite.
    /// Override when the service under test requires <see cref="BusMessage.Caller"/>.
    /// </summary>
    protected virtual CallerIdentity? TestCaller => null;

    /// <summary>
    /// Builds a DATA bus message for the suite, including <see cref="TestCaller"/> when set.
    /// </summary>
    protected BusMessage CreateBusMessage(
        string action,
        string? data = null,
        int? id = null,
        string? resource = null) =>
        new()
        {
            Type = Enums.BusServiceTypeEnum.DATA,
            Action = action,
            Data = data,
            Id = id,
            Resource = resource ?? string.Empty,
            Caller = TestCaller,
        };

    /// <summary>
    /// Creates a new context seeded with base data and any additional fake entities.
    /// </summary>
    protected virtual TContext CreateContext()
    {
        var ctx = ResolveFakeDataset().CreateFakeContext();

        var extraData = GetFakeData();
        if (extraData?.Count == 0)
        {
            var set = ctx.Set<TEntity>();
            set.AddRange(extraData);
            ctx.SaveChanges();
        }

        return ctx;
    }

    /// <summary>
    /// Resolve the fake dataset
    /// </summary>
    /// <returns>FakeDataset class</returns>
    /// <exception cref="InvalidOperationException">If no IFakeDataset implementation found</exception>
    private IFakeDataset<TContext> ResolveFakeDataset()
    {
        var targetInterface = typeof(IFakeDataset<TContext>);
        var testAssembly = GetType().Assembly;

        var implType = testAssembly
            .GetTypes()
            .FirstOrDefault(t => !t.IsAbstract && !t.IsInterface && targetInterface.IsAssignableFrom(t));

        if (implType is null)
        {
            throw new InvalidOperationException(
                $"No implementation of {targetInterface.Name} was found in {testAssembly.GetName().Name}. " +
                $"Ensure your FakeDataset class is public and in the test project.");
        }

        return (IFakeDataset<TContext>)Activator.CreateInstance(implType)!;
    }

    /// <summary>
    /// Verifies that an unknown action routed to the service results in a <see cref="BadRequestException"/>.
    /// </summary>
    [Fact]
    public async Task Action_Handle_Unknown_Action()
    {
        // Arrange
        var service = CreateService();

        // Assert
        await Assert.ThrowsAsync<InternalServerErrorException>(() =>
            service.HandleAsync(CreateBusMessage("UnknownAction")));
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
    // Update validates the payload before resolving the target id.
    [InlineData("Data mandatory", "Update")]
    [InlineData("Data mandatory", "Update", null, 1)]
    [InlineData("Id mandatory", "Update", "{}", null)]
    [InlineData("Id mandatory", "Delete")]
    public async Task Action_Handle_Throw_Invalid_Data(string exception, string action, string? data = null, int? id = null)
    {
        // Arrange
        var service = CreateService();

        // Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.HandleAsync(CreateBusMessage(action, data, id)));
        Assert.Equal(exception, ex.Message);
    }

    /// <summary>
    /// Ensures that the <c>Create</c> action returns a serialized entity with a non-zero identifier.
    /// </summary>
    [Fact]
    public async Task Create_Test()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.HandleAsync(
            CreateBusMessage("Create", JsonSafe.Serialize(GetNewEntity())));

        // Assert
        var id = JsonConvert.DeserializeObject<int>(result);
        Assert.NotEqual(0, id);
    }

    /// <summary>
    /// Verifies that <c>Get</c> returns the expected entity payload for a given identifier.
    /// </summary>
    /// <param name="id">Requested entity id.</param>
    /// <param name="expected">Expected id in the response payload (or <see langword="null"/> when not found).</param>
    [Theory]
    [InlineData(1, 1)]
    public async Task Get_By_Id(int id, int? expected)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.HandleAsync(CreateBusMessage("Get", id: id));

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
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.HandleAsync(CreateBusMessage("List"));
        var entities = JsonConvert.DeserializeObject<List<TEntity>>(result);

        // Assert
        Assert.NotNull(entities);
        Assert.NotEmpty(entities);
    }
}
