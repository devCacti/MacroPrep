using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MacroPrep.Server.Services
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var id = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? connection.User?.FindFirst("sub")?.Value;

            // 2. DEBUG LOGGING (Check your server console when the CLIENT connects)
            if (!string.IsNullOrEmpty(id))
            {
                Console.WriteLine($"\n > [SignalR] Mapping Connection {connection.ConnectionId} to User ID: '{id}'\n");
            }
            else
            {
                Console.WriteLine($"\n > [SignalR] Connection {connection.ConnectionId} has NO USER ID!\n");
            }

            return id;
        }
    }
}
