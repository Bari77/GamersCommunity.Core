namespace GamersCommunity.Core.Services
{
    public interface ITableService
    {
        /// <summary>
        /// Table name
        /// </summary>
        string TableName { get; }

        /// <summary>
        /// Method called when you handle an operation
        /// </summary>
        /// <param name="action">CRUD Action (Create / Get / List / Update / Delete)</param>
        /// <param name="data">Data for action</param>
        /// <param name="id">Id of data</param>
        /// <param name="ct">CancellationToken of request</param>
        /// <returns>String with value</returns>
        Task<string> HandleAsync(string action, string data, int? id, CancellationToken ct);
    }

}
