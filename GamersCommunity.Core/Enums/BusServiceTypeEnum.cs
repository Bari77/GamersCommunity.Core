namespace GamersCommunity.Core.Enums
{
    public enum BusServiceTypeEnum
    {
        /// <summary>
        /// Standard data-backed service (CRUD tables, persisted entities, etc.)
        /// </summary>
        DATA,

        /// <summary>
        /// Application-level services exposing business or configuration logic
        /// not directly tied to a database table (e.g. AppSettings, Reports, Billing).
        /// </summary>
        APP,

        /// <summary>
        /// Infrastructure or technical services supporting the system
        /// (e.g. Cache, Notifications, Logging).
        /// </summary>
        INFRA
    }
}
