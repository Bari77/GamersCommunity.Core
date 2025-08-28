using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public class UnauthorizedException(string message) : Exception(message), IAppException
    {
        public HttpStatusCode Code => HttpStatusCode.Unauthorized;
    }
}
