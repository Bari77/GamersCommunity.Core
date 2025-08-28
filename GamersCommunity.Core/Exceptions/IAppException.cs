using System.Net;

namespace GamersCommunity.Core.Exceptions
{
    public interface IAppException
    {
        public HttpStatusCode Code { get; }
    }
}
