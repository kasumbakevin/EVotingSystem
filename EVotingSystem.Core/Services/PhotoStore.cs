namespace EVotingSystem.Services
{
    /// <summary>
    /// Stores voter reference photographs and captured verification images on disk
    /// under a single controlled directory. Images are never written to the
    /// database and are kept separate from ballot data.
    /// </summary>
    public sealed class PhotoStore
    {
        public string RootDirectory { get; }

        public PhotoStore(string? rootDirectory = null)
        {
            RootDirectory = rootDirectory ?? DefaultDirectory();
            Directory.CreateDirectory(Path.Combine(RootDirectory, "reference"));
            Directory.CreateDirectory(Path.Combine(RootDirectory, "captures"));
        }

        public static string DefaultDirectory()
        {
            var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDirectory, "EVotingSystem", "photos");
        }

        public string SaveReference(int voterId, byte[] imageBytes, string extension = ".png")
        {
            var path = Path.Combine(RootDirectory, "reference", $"voter_{voterId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}");
            File.WriteAllBytes(path, imageBytes);
            return path;
        }

        public string SaveCapture(int voterId, byte[] imageBytes, string extension = ".png")
        {
            var path = Path.Combine(RootDirectory, "captures", $"voter_{voterId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}");
            File.WriteAllBytes(path, imageBytes);
            return path;
        }

        public byte[]? TryRead(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            return File.ReadAllBytes(path);
        }
    }
}
