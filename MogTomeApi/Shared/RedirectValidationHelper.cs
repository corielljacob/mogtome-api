namespace MogTomeApi.Shared
{
    public class RedirectValidationHelper
    {
        public static bool DetermineIfRedirectIsValid(string redirect, string[] allowedHosts)
        {
            if (string.IsNullOrEmpty(redirect))
                return false;

            if (Uri.TryCreate(redirect, UriKind.Absolute, out var redirectUri) == false)
            {
                return false;
            }

            if (redirectUri.Scheme != Uri.UriSchemeHttps && redirectUri.Host != "localhost")
                return false;

            if (string.IsNullOrEmpty(redirectUri.UserInfo) == false)
                return false;

            if (allowedHosts.Contains(redirectUri.Host, StringComparer.OrdinalIgnoreCase) == false)
            {
                return false;
            }

            return true;
        }
    }
}
