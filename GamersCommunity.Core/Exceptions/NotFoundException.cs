using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public class NotFoundException(string message) : Exception(message), IAppException
    {
        public HttpStatusCode Code => HttpStatusCode.NotFound;
    }
}
