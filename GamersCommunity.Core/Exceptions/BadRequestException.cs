using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public class BadRequestException(string message) : Exception(message), IAppException
    {
        public HttpStatusCode Code => HttpStatusCode.BadRequest;
    }
}
