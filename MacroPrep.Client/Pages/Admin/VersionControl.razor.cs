namespace MacroPrep.Client.Pages.Admin
{
    public partial class VersionControl
    {
        private bool _isLoading = true;
        private List<AppVersionRecord> _versions = [];

        // Editor State
        private AppVersionRecord? _editingVersion;
        private bool _isNewVersion = false;

        private AppVersionRecord? ActiveVersion => _versions.FirstOrDefault(v => v.IsActive);

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Simulate fetching from server
                await Task.Delay(1000);

                // Mock Data
                _versions = new List<AppVersionRecord>
            {
                new() { Id = 1, Major = 0, Minor = 9, Patch = 8, Hash = "f8a9c21", ForceRefresh = false, IsActive = false },
                new() { Id = 2, Major = 0, Minor = 9, Patch = 10, Hash = "b4e71df", ForceRefresh = true, IsActive = true }
            };
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
            _editingVersion = new AppVersionRecord
            {
                // Pre-fill with the latest version + 1 patch by default to save typing
                Major = latest?.Major ?? 1,
                Minor = latest?.Minor ?? 0,
                Patch = (latest?.Patch ?? 0) + 1,
                ForceRefresh = false,
                IsActive = false
            };
        }

        private void EditVersion(AppVersionRecord version)
        {
            _isNewVersion = false;
            // Create a copy so we don't mutate the table immediately before saving
            _editingVersion = new AppVersionRecord
            {
                Id = version.Id,
                Major = version.Major,
                Minor = version.Minor,
                Patch = version.Patch,
                Hash = version.Hash,
                ForceRefresh = version.ForceRefresh,
                IsActive = version.IsActive
            };
        }

        private void CancelEdit()
        {
            _editingVersion = null;
            _isNewVersion = false;
        }

        private async Task SaveChanges()
        {
            if (_editingVersion == null) return;

            _isLoading = true;

            // Simulate API Save
            await Task.Delay(800);

            if (_isNewVersion)
            {
                // Fake getting a new ID from DB
                _editingVersion.Id = _versions.Any() ? _versions.Max(v => v.Id) + 1 : 1;
                _versions.Add(_editingVersion);
            }
            else
            {
                var existing = _versions.FirstOrDefault(v => v.Id == _editingVersion.Id);
                if (existing != null)
                {
                    existing.Major = _editingVersion.Major;
                    existing.Minor = _editingVersion.Minor;
                    existing.Patch = _editingVersion.Patch;
                    existing.Hash = _editingVersion.Hash;
                    existing.ForceRefresh = _editingVersion.ForceRefresh;
                }
            }

            _editingVersion = null;
            _isNewVersion = false;
            _isLoading = false;
        }

        private async Task SetActiveVersion(AppVersionRecord targetVersion)
        {
            _isLoading = true;
            await Task.Delay(500); // Simulate API call to update active status

            foreach (var v in _versions)
            {
                v.IsActive = (v.Id == targetVersion.Id);
            }

            _isLoading = false;
        }

        // Inner class for the UI model (You will likely map this to a shared DTO later)
        private class AppVersionRecord
        {
            public int Id { get; set; }
            public int Major { get; set; }
            public int Minor { get; set; }
            public int Patch { get; set; }
            public string Hash { get; set; } = string.Empty;
            public bool ForceRefresh { get; set; }
            public bool IsActive { get; set; }

            public string GetSemanticVersion() => $"{Major}.{this.Minor}.{this.Patch}";
        }
    }
}
