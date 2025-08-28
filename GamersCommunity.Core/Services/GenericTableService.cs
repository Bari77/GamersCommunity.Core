using GamersCommunity.Core.Database;
using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Rabbit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace GamersCommunity.Core.Services
{
    public class GenericTableService<TContext, TEntity>(TContext context, string tableName) : ITableService
        where TContext : DbContext
        where TEntity : class, IKeyTable
    {
        private readonly TContext _context = context;

        public string TableName => tableName;

        public async Task<string> HandleAsync(string action, string? data = null, int? id = null)
        {
            switch (action)
            {
                case "Create":
                    if (string.IsNullOrEmpty(data))
                    {
                        throw new BadRequestException("Data mandatory");
                    }
                    var create = ConsumerParamParser.ToObject<TEntity>(data);
                    return JsonConvert.SerializeObject(await CreateAsync(create));

                case "Get":
                    if (!id.HasValue)
                    {
                        throw new BadRequestException("Id mandatory");
                    }
                    return JsonConvert.SerializeObject(await GetAsync(id.Value));

                case "List":
                    return JsonConvert.SerializeObject(await ListAsync());

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
                    return JsonConvert.SerializeObject(await UpdateAsync(id.Value, update));

                case "Delete":
                    if (!id.HasValue)
                    {
                        throw new BadRequestException("Id mandatory");
                    }
                    return JsonConvert.SerializeObject(await DeleteAsync(id.Value));

                default:
                    Log.Warning($"Action {action} not implemented");
                    throw new InternalServerErrorException($"Action {action} not implemented");
            }
        }

        private async Task<int> CreateAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity.Id;
        }

        private async Task<TEntity> GetAsync(int id)
        {
            return await _context.Set<TEntity>().FirstOrDefaultAsync(w => w.Id == id) ?? throw new NotFoundException("Cannot find ressource");
        }

        private async Task<List<TEntity>> ListAsync()
        {
            return await _context.Set<TEntity>().ToListAsync();
        }

        private async Task<bool> UpdateAsync(int id, TEntity entity)
        {
            _context.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> DeleteAsync(int id)
        {
            var data = await GetAsync(id);
            _context.Remove(data);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
