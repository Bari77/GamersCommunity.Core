using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public class RequestTimeoutException(string message) : Exception(message), IAppException
    {
        public HttpStatusCode Code => HttpStatusCode.RequestTimeout;
    }
}
