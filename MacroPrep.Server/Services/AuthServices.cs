using MacroPrep.Server.Data;
using MacroPrep.Server.Data.Entities;
using MacroPrep.Server.Endpoints;
using MacroPrep.Shared.Interfaces;
using MacroPrep.Shared.Models.Auth;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;

namespace MacroPrep.Server.Services
{
    public class AuthServices
    {
        public AuthServices() { }


    }

    public static class AuthServicesExtensions
    {
        // Authentication Checks

        // Registration Check
        /// <summary>
        /// Checks if the registration request is empty or contains invalid data.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>
        /// True if the registration request is empty or contains invalid data (e.g., missing username or password), false otherwise.
        /// </returns>
        public static bool EmptyCheck(this IAuthInterface request)
        {
            if (request == null)
                return true;

            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
                return true;

            if (request is RegisterRequestDto registerRequest)
            {
                if (string.IsNullOrWhiteSpace(registerRequest.Email) || string.IsNullOrWhiteSpace(registerRequest.ConfirmPassword))
                    return true;
            }

            return false;
        }

        // Conflict Check
        /// <summary>
        /// Checks if there is a user with the same username or email already exists in the database.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>
        /// True if there is a user with the same username or email already exists in the database, false otherwise.
        /// </returns>
        public static async Task<bool> ExistsCheck(this IAuthInterface request, AppDbContext context)
        {
            if (request.EmptyCheck())
                return false;

            // Determine the email to check based on the type of request
            // Since the in the Login, the user can use either username or email, we need to check both fields for conflicts
            // If it's a RegisterRequestDto, we use the actual email field, otherwise we just use the UserName field which might be the email
            string email = request is RegisterRequestDto registerRequest ? registerRequest.Email : request.UserName;

            bool conflict = await context.Users.AnyAsync(u => u.UserName == request.UserName || u.Email == email);

            if (conflict)
                return true;

            // no conflict found
            return false;
        }

        public static async Task<User?> GetUser(this IAuthInterface request, AppDbContext context)
        {
            if (request.EmptyCheck())
                return null;

            return await context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName || u.Email == request.UserName);
        }

        public static async Task<(bool isValid, User? user)> ValidateCredentials(this LoginRequestDto request, AppDbContext context)
        {
            try
            {
                if (request.EmptyCheck())
                    throw new InvalidOperationException("Login request is empty or contains invalid data.");

                // Check if the user exists with the provided username or email
                User user = await request.GetUser(context) ??
                    throw new InvalidOperationException("User not found.");

                // Validate the password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    throw new UnauthorizedAccessException("Invalid password.");

                return (true, user);
            }
            catch (Exception) { return (false, null); }

            // All checks passed, credentials are valid
        }

        public static async Task<UserSession?> CreateSessionAsync(this User user, AppDbContext context)
        {
            if (user == null)
                return null;

            UserSession session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(), // In a real application, you would want to use a more secure token generation method
                CreatedAt = DateTimeOffset.UtcNow,
                IsRevoked = false
            };
            context.UserSessions.Add(session);
            await context.SaveChangesAsync();

            return session;
        }

        public static void SetSessionCookie(this UserSession session, HttpContext httpContext)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = AuthEndpoints.IsProduction ? SameSiteMode.Strict : SameSiteMode.None,
                Expires = session.ExpiresAt,
                Path = "/api/auth",
                IsEssential = !AuthEndpoints.IsProduction
            };

            httpContext.Response.Cookies.Append("MacroPrepSession", $"{session.Id}|{session.Token}", cookieOptions);
        }

        public static async Task<bool> CheckSession(this UserSession? session, AppDbContext db, string sessionToken)
        {
            if (session == null || session.User == null || session.IsRevoked || session.ExpiresAt < DateTimeOffset.UtcNow || session.Token != sessionToken)
            {
                if (session != null)
                {
                    session.IsRevoked = true; // Revoke the session if token is invalid or expired.
                    await db.SaveChangesAsync();
                }
                return false;
            }
            return true;
        }
    }
}