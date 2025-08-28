using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public class ForbiddenException(string message) : Exception(message), IAppException
    {
        public HttpStatusCode Code => HttpStatusCode.Forbidden;
    }
}
