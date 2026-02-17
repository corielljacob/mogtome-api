using System.Security.Cryptography;

namespace MogTomeApi.Shared
{
    public class SessionHelper
    {
        public static string GenerateSessionId()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }
    }
}
