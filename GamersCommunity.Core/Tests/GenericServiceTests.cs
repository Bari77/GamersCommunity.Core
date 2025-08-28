using GamersCommunity.Core.Database;
using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;

namespace GamersCommunity.Core.Tests
{
    public abstract class GenericServiceTests<TContext, TService, TEntity>(TService service)
        where TService : GenericTableService<TContext, TEntity>
        where TContext : DbContext
        where TEntity : class, IKeyTable
    {
        private readonly TService _service = service;

        protected abstract List<TEntity> GetFakeData();

        protected abstract TEntity GetNewEntity();

        [Fact]
        public async Task Action_Handle_Unknown_Action()
        {
            // Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _service.HandleAsync("UnknownAction", string.Empty));
        }

        [Theory]
        [InlineData("Data mandatory", "Create")]
        [InlineData("Id mandatory", "Get")]
        [InlineData("Id mandatory", "Update")]
        [InlineData("Data mandatory", "Update", null, 1)]
        [InlineData("Id mandatory", "Delete")]
        public async Task Action_Handle_Throw_Invalid_Data(string exception, string action, string? data = null, int? id = null)
        {
            // Assert
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _service.HandleAsync(action, data, id));
            Assert.Equal(exception, ex.Message);
        }

        [Fact]
        public async Task Create_Test()
        {
            // Act
            var result = await _service.HandleAsync("Create", JsonConvert.SerializeObject(GetNewEntity()));

            // Assert
            var entity = JsonConvert.DeserializeObject<TEntity>(result);
            Assert.NotNull(entity);
            Assert.NotEqual(0, entity.Id);
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(1, 1)]
        public async Task Get_By_Id(int id, int? expected)
        {
            // Act
            var result = await _service.HandleAsync("Get", id: id);

            // Assert
            var entity = JsonConvert.DeserializeObject<TEntity>(result);
            Assert.Equal(expected, entity?.Id);
        }

        [Fact]
        public async Task List_All()
        {
            // Act
            var result = await _service.HandleAsync("List", string.Empty);
            var entities = JsonConvert.DeserializeObject<List<TEntity>>(result);

            // Assert
            Assert.NotNull(entities);
            Assert.Equal(GetFakeData().Count, entities.Count);
        }
    }
}
