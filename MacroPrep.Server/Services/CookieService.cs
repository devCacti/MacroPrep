namespace MacroPrep.Server.Services
{
    public interface ICookieService
    {
        
    }

    public class CookieService
    {
        CookieService() {}

        public static string CookieName = "MacroPrepSession";

        public static (Guid? SessionId, string? Token) DecodeCookie(string? cookie)
        {
            try
            {       
                if (string.IsNullOrEmpty(cookie))
                    throw new();

                string[]? cookieParts = cookie.Split("|");

                Guid sessionId = Guid.Parse(cookieParts[0]);
                string token = cookieParts[1];

                return (sessionId, token);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}