using GamersCommunity.Core.Database;
using GamersCommunity.Core.Enums;
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
    /// This service implements the <see cref="IBusService"/> contract for a single logical table,
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
    public class GenericDataService<TContext, TEntity>(TContext context, string tableName) : IBusService
        where TContext : DbContext
        where TEntity : class, IKeyTable
    {
        /// <inheritdoc/>
        BusServiceTypeEnum IBusService.Type => BusServiceTypeEnum.DATA;
        /// <inheritdoc/>
        public string Resource => tableName;
        /// <summary>
        /// Database context
        /// </summary>
        protected TContext Context => context;

        /// <inheritdoc/>
        public virtual async Task<string> HandleAsync(BusMessage message, CancellationToken ct = default)
        {
            switch (message.Action.ToUpperInvariant())
            {
                case "CREATE":
                    if (string.IsNullOrEmpty(message.Data))
                    {
                        throw new BadRequestException("DATA_MANDATORY", "Data mandatory");
                    }
                    var create = ConsumerParamParser.ToObject<TEntity>(message.Data);
                    return JsonSafe.Serialize(await CreateAsync(create, ct));

                case "GET":
                    if (!message.Id.HasValue)
                    {
                        throw new BadRequestException("ID_MANDATORY", "Id mandatory");
                    }
                    return JsonSafe.Serialize(await GetAsync(message.Id.Value, ct));

                case "LIST":
                    return JsonSafe.Serialize(await ListAsync(ct));

                case "UPDATE":
                    if (!message.Id.HasValue)
                    {
                        throw new BadRequestException("ID_MANDATORY", "Id mandatory");
                    }
                    if (string.IsNullOrEmpty(message.Data))
                    {
                        throw new BadRequestException("DATA_MANDATORY", "Data mandatory");
                    }
                    var update = ConsumerParamParser.ToObject<TEntity>(message.Data);
                    return JsonSafe.Serialize(await UpdateAsync(message.Id.Value, update, ct));

                case "DELETE":
                    if (!message.Id.HasValue)
                    {
                        throw new BadRequestException("ID_MANDATORY", "Id mandatory");
                    }
                    return JsonSafe.Serialize(await DeleteAsync(message.Id.Value, ct));

                default:
                    Log.Warning($"Action {message.Action} not implemented");
                    throw new InternalServerErrorException("ACTION_NOT_IMPLEMENTED", $"Action {message.Action} not implemented");
            }
        }

        /// <summary>
        /// Persists a new entity and returns its generated identifier.
        /// </summary>
        /// <param name="entity">The entity to create.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created entity identifier.</returns>
        protected async Task<int> CreateAsync(TEntity entity, CancellationToken ct = default)
        {
            await Context.Set<TEntity>().AddAsync(entity, ct);
            await Context.SaveChangesAsync(ct);
            return entity.Id;
        }

        /// <summary>
        /// Retrieves a single entity by its identifier.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The matching entity.</returns>
        /// <exception cref="NotFoundException">Thrown when no entity matches <paramref name="id"/>.</exception>
        protected async Task<TEntity> GetAsync(int id, CancellationToken ct = default)
        {
            return await Context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct)
                   ?? throw new NotFoundException("NOT_FOUND", "Cannot find ressource");
        }

        /// <summary>
        /// Returns all entities of the configured set.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of entities.</returns>
        protected async Task<List<TEntity>> ListAsync(CancellationToken ct = default)
        {
            return await Context.Set<TEntity>().AsNoTracking().ToListAsync(ct);
        }

        /// <summary>
        /// Updates an entity and persists changes.
        /// </summary>
        /// <param name="id">Identifier of the entity to update (informational; the passed entity's state is applied).</param>
        /// <param name="entity">Entity instance carrying the new values.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see langword="true"/> when the operation completes.</returns>
        protected async Task<bool> UpdateAsync(int id, TEntity entity, CancellationToken ct = default)
        {
            Context.Update(entity);
            await Context.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// Deletes the entity identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Identifier of the entity to remove.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see langword="true"/> when the operation completes.</returns>
        protected async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var data = await GetAsync(id, ct);
            Context.Remove(data);
            await Context.SaveChangesAsync(ct);
            return true;
        }
    }
}
