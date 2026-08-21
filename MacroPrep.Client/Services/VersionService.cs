using MacroPrep.Shared.Models.SemanticVersioning;
using System.Net.Http.Json;

namespace MacroPrep.Client.Services
{
    public class VersionService
    {
        private readonly HttpClient Http;

        public VersionService(HttpClient http)
        {
            Http = http;
        }

        public async Task<SemanticVersionDto?> GetActiveVersionAsync()
        {
            SemanticVersionDto? version = null;

            try
            {
                var response = await Http.GetAsync("api/version/client");
                if (response.IsSuccessStatusCode)
                {
                    version = await response.Content.ReadFromJsonAsync<SemanticVersionDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem at GET: Active Version" + ex.ToString());
            }
            
            return version;
        }

        public async Task<SemanticVersionsPagesDto> GetAllVersionsAsync()
        {
            SemanticVersionsPagesDto versionsPages = new SemanticVersionsPagesDto();
            try
            {
                var response = await Http.GetAsync("api/version");
                if (response.IsSuccessStatusCode)
                {
                    var versionsPagesDto = await response.Content.ReadFromJsonAsync<SemanticVersionsPagesDto>();
                    versionsPages = versionsPagesDto ?? new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem at GET: All Versions" + ex.ToString());
            }
            return versionsPages;
        }

        public async Task<SemanticVersionDto?> UploadNewVersion(SemanticVersionDto version)
        {
            SemanticVersionDto? newVersion = null;

            try
            {
                var response = await Http.PostAsJsonAsync("api/version", version);
                if (response.IsSuccessStatusCode)
                {
                    newVersion = await response.Content.ReadFromJsonAsync<SemanticVersionDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem at POST: New Version" + ex.ToString());
            }

            return newVersion;
        }

        public async Task<SemanticVersionDto?> UpdateVersionAsync(SemanticVersionDto version)
        {
            SemanticVersionDto? updatedVersion = null;
            try
            {
                var response = await Http.PutAsJsonAsync($"api/version/{version.VersionID}", version);
                if (response.IsSuccessStatusCode)
                {
                    updatedVersion = await response.Content.ReadFromJsonAsync<SemanticVersionDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem at PUT: Update Version" + ex.ToString());
            }
            return updatedVersion;
        }
        
        public async Task<bool> SetVersionActiveAsync(Guid versionId)
        {
            // This method sends a PATCH request just to set the version as active.
            try
            {
                // Only different one as it does not use the regular api/version endpoint
                var response = await Http.PatchAsync($"api/version/set-active?{nameof(versionId)}={versionId}", null);

                // Server returns 204 No Content on success
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem at PATCH: Set Version Active" + ex.ToString());
            }

            // If it failed, we return false to indicate failure
            return false;
        }

        public async Task<bool> DeleteVersionAsync(Guid versionId)
        {
            try
            {
                var response = await Http.DeleteAsync($"api/version/{versionId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem at DELETE: Delete Version" + ex.ToString());
                return false;
            }
        }

    }
}
