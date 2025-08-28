using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public class GatewayTimeoutException(string message) : Exception(message), IAppException
    {
        public HttpStatusCode Code => HttpStatusCode.GatewayTimeout;
    }
}
