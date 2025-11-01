using GamersCommunity.Core.Database;
using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Rabbit;
using GamersCommunity.Core.Serialization;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GamersCommunity.Core.Services
{
    /// <summary>
    /// Generic CRUD table service backed by an <see cref="DbContext"/> and a keyed entity type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service implements the <see cref="ITableService"/> contract for a single logical table,
    /// translating incoming actions (<c>Create</c>, <c>Get</c>, <c>List</c>, <c>Update</c>, <c>Delete</c>)
    /// into EF Core operations on <typeparamref name="TEntity"/>.
    /// </para>
    /// <para>
    /// Payloads are exchanged as JSON strings through the RabbitMQ pipeline; this service relies on
    /// <see cref="ConsumerParamParser"/> to deserialize the <c>data</c> into the expected CLR type.
    /// </para>
    /// </remarks>
    /// <typeparam name="TContext">A concrete <see cref="DbContext"/> used to access the database.</typeparam>
    /// <typeparam name="TEntity">
    /// An entity type tracked by <typeparamref name="TContext"/> implementing <see cref="IKeyTable"/> (i.e., exposing <c>Id</c>).
    /// </typeparam>
    /// <param name="context">Database context instance used for CRUD operations.</param>
    /// <param name="tableName">Logical table name advertised to the router.</param>
    public class GenericTableService<TContext, TEntity>(TContext context, string tableName) : ITableService
        where TContext : DbContext
        where TEntity : class, IKeyTable
    {
        /// <summary>
        /// Logical name used by the router to match incoming messages to this service.
        /// </summary>
        public string TableName => tableName;

        /// <summary>
        /// Dispatches the requested <paramref name="action"/> to the corresponding CRUD method and
        /// returns a JSON-serialized result.
        /// </summary>
        /// <param name="action">
        /// One of <c>Create</c>, <c>Get</c>, <c>List</c>, <c>Update</c>, <c>Delete</c>.
        /// </param>
        /// <param name="data">
        /// Optional JSON payload. Required for <c>Create</c> and <c>Update</c> (represents a <typeparamref name="TEntity"/> instance).
        /// </param>
        /// <param name="id">
        /// Optional identifier. Required for <c>Get</c>, <c>Update</c>, and <c>Delete</c>.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>JSON string containing the operation result.</returns>
        /// <exception cref="BadRequestException">
        /// Thrown when required inputs are missing or cannot be parsed from <paramref name="data"/>.
        /// </exception>
        /// <exception cref="InternalServerErrorException">
        /// Thrown when <paramref name="action"/> is not recognized/implemented.
        /// </exception>
        public async Task<string> HandleAsync(string action, string? data = null, int? id = null, CancellationToken ct = default)
        {
            switch (action)
            {
                case "Create":
                    if (string.IsNullOrEmpty(data))
                    {
                        throw new BadRequestException("Data mandatory");
                    }
                    var create = ConsumerParamParser.ToObject<TEntity>(data);
                    return JsonSafe.Serialize(await CreateAsync(create, ct));

                case "Get":
                    if (!id.HasValue)
                    {
                        throw new BadRequestException("Id mandatory");
                    }
                    return JsonSafe.Serialize(await GetAsync(id.Value, ct));

                case "List":
                    return JsonSafe.Serialize(await ListAsync(ct));

                case "Update":
                    if (!id.HasValue)
                    {
                        throw new BadRequestException("Id mandatory");
                    }
                    if (string.IsNullOrEmpty(data))
                    {
                        throw new BadRequestException("Data mandatory");
                    }
                    var update = ConsumerParamParser.ToObject<TEntity>(data);
                    return JsonSafe.Serialize(await UpdateAsync(id.Value, update, ct));

                case "Delete":
                    if (!id.HasValue)
                    {
                        throw new BadRequestException("Id mandatory");
                    }
                    return JsonSafe.Serialize(await DeleteAsync(id.Value, ct));

                default:
                    Log.Warning($"Action {action} not implemented");
                    throw new InternalServerErrorException($"Action {action} not implemented");
            }
        }

        /// <summary>
        /// Persists a new entity and returns its generated identifier.
        /// </summary>
        /// <param name="entity">The entity to create.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created entity identifier.</returns>
        private async Task<int> CreateAsync(TEntity entity, CancellationToken ct = default)
        {
            await context.Set<TEntity>().AddAsync(entity, ct);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        /// <summary>
        /// Retrieves a single entity by its identifier.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The matching entity.</returns>
        /// <exception cref="NotFoundException">Thrown when no entity matches <paramref name="id"/>.</exception>
        private async Task<TEntity> GetAsync(int id, CancellationToken ct = default)
        {
            return await context.Set<TEntity>().FirstOrDefaultAsync(w => w.Id == id, ct)
                   ?? throw new NotFoundException("Cannot find ressource");
        }

        /// <summary>
        /// Returns all entities of the configured set.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of entities.</returns>
        private async Task<List<TEntity>> ListAsync(CancellationToken ct = default)
        {
            return await context.Set<TEntity>().ToListAsync(ct);
        }

        /// <summary>
        /// Updates an entity and persists changes.
        /// </summary>
        /// <param name="id">Identifier of the entity to update (informational; the passed entity's state is applied).</param>
        /// <param name="entity">Entity instance carrying the new values.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see langword="true"/> when the operation completes.</returns>
        private async Task<bool> UpdateAsync(int id, TEntity entity, CancellationToken ct = default)
        {
            context.Update(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// Deletes the entity identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Identifier of the entity to remove.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see langword="true"/> when the operation completes.</returns>
        private async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var data = await GetAsync(id, ct);
            context.Remove(data);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}
