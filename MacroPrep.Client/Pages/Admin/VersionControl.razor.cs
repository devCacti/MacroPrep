using MacroPrep.Client.Services;
using MacroPrep.Shared.Models.SemanticVersioning;
using Microsoft.AspNetCore.Components;
using System.Reflection.Metadata.Ecma335;
using System.Transactions;

namespace MacroPrep.Client.Pages.Admin
{
    public partial class VersionControl
    {
        private bool _isLoading = true;
        private List<SemanticVersionDto> _versions = [];

        // Services
        [Inject] private VersionService VersionService { get; set; } = default!;

        // Editor State
        private SemanticVersionDto? _editingVersion;
        private bool _isNewVersion = false;

        private SemanticVersionDto? ActiveVersion => _versions.FirstOrDefault(v => v.ActiveVersion);

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Feth Active Version at GET: url/api/version/client
                var activeVersion = await VersionService.GetActiveVersionAsync();

                var allVersions = await VersionService.GetAllVersionsAsync();

                if (allVersions != null) {
                    _versions = [.. allVersions.Versions.Select(v => new SemanticVersionDto
                    {
                        VersionID = v.VersionID,
                        Major = v.Major,
                        Minor = v.Minor,
                        Patch = v.Patch,
                        Hash = v.Hash ?? string.Empty,
                        ForceRefresh = v.ForceRefresh,
                        ActiveVersion = v.ActiveVersion
                    })];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load versions: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void CreateNewVersion()
        {
            var latest = _versions.OrderByDescending(v => v.Major).ThenByDescending(v => v.Minor).ThenByDescending(v => v.Patch).FirstOrDefault();

            _isNewVersion = true;
            _editingVersion = new SemanticVersionDto
            {
                // Pre-fill with the latest version + 1 patch by default to save typing
                VersionID = Guid.NewGuid(),
                Major = latest?.Major ?? 0,
                Minor = latest?.Minor ?? 0,
                Patch = (latest?.Patch ?? 0) + 1,
                ForceRefresh = false,
                ActiveVersion = true
            };
        }

        private void EditVersion(SemanticVersionDto version)
        {
            _isNewVersion = false;
            // Create a copy so we don't mutate the table immediately before saving
            _editingVersion = new SemanticVersionDto
            {
                VersionID = version.VersionID,
                Major = version.Major,
                Minor = version.Minor,
                Patch = version.Patch,
                Hash = version.Hash,
                ForceRefresh = version.ForceRefresh,
                ActiveVersion = version.ActiveVersion
            };
        }

        private void CancelEdit()
        {
            _editingVersion = null;
            _isNewVersion = false;
        }

        // Save the changes made in the editor to the list of versions (and server)
        private async Task SaveChanges()
        {
            if (_editingVersion == null) return;

            _isLoading = true;

            if (_isNewVersion)
            {
                // Upload the new version to the server
                _editingVersion.ActiveVersion = false; // New versions are not active by default, though the client decides

                var _version = await VersionService.UploadNewVersion(_editingVersion);

                if (_version != null)
                {
                    _versions.Add(_version);
                }
            }
            else 
            {
                int vIndex = _versions.FindIndex(v => v.VersionID == _editingVersion.VersionID);

                // Update the existing version on the server
                if (vIndex >= 0)
                {
                    var updatedVersion = await VersionService.UpdateVersionAsync(_editingVersion);

                    if (updatedVersion != null)
                    {
                        _versions[vIndex] = updatedVersion;
                    }
                    else
                        _versions.RemoveAt(vIndex); // If update failed, remove it from the list
                    // Likely either doesn't exist or something went wrong
                    // This way we stop the user from editing again and wasting more resources.
                }
            }

            _editingVersion = null;
            _isNewVersion = false;
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task SetActiveVersion(Guid versionId)
        {
            _isLoading = true;

            // Set the current version as active
            bool success = await VersionService.SetVersionActiveAsync(versionId);

            var version = _versions.FirstOrDefault(v => v.VersionID == versionId);

            if (version != null)
            {
                foreach (var v in _versions)
                {
                    v.ActiveVersion = false; // Reset all to false
                }

                // Set the selected version as active
                version.ActiveVersion = success;
            }

            _isLoading = false;
        }

        private async Task DeleteVersion(Guid versionId)
        {
            _isLoading = true;

            bool success = await VersionService.DeleteVersionAsync(versionId);

            if (success)
                _versions.RemoveAll(v => v.VersionID == versionId);

            _isLoading = false;
        }
    }
}
