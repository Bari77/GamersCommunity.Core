using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public class InternalServerErrorException(string message) : Exception(message), IAppException
    {
        public HttpStatusCode Code => HttpStatusCode.InternalServerError;
    }
}
