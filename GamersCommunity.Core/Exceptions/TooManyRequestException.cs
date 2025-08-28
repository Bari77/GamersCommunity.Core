using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public class TooManyRequestsException(string message) : Exception(message), IAppException
    {
        public HttpStatusCode Code => HttpStatusCode.TooManyRequests;
    }
}
