using System;
using System.IO;
using System.Text.RegularExpressions;

namespace UmbraMenu
{
    /// <summary>Per-user, per-project JSON storage. Serialization and validation belong to the caller.</summary>
    public sealed class SettingsStore
    {
        public string Folder { get; private set; }
        public string Path { get { return PathFor("settings.json"); } }

        /// <param name="projectId">Stable folder ID, such as Umbra. Not a display title or a path.</param>
        /// <param name="appDataRoot">Optional portable/test root; normally leave null for the current user's ApplicationData.</param>
        public SettingsStore(string projectId = ProjectConfig.GameName, string appDataRoot = null)
        {
            ValidateName(ProjectConfig.SettingsBase, nameof(ProjectConfig.SettingsBase));
            ValidateName(projectId, nameof(projectId));
            string root = appDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(root) || !System.IO.Path.IsPathRooted(root))
                throw new ArgumentException("Application data must be an absolute directory.", nameof(appDataRoot));
            Folder = System.IO.Path.Combine(root, ProjectConfig.SettingsBase, "Games", projectId);
        }

        public string PathFor(string fileName)
        {
            ValidateName(fileName, nameof(fileName));
            return System.IO.Path.Combine(Folder, fileName);
        }

        /// <summary>Reads primary then backup, validating with accept. Optional legacy import never deletes old files.</summary>
        public bool Read(Action<string> accept, out string warning, string fileName = "settings.json", string legacyPath = null)
        { return SettingsFile.Read(PathFor(fileName), accept, out warning, legacyPath); }

        /// <summary>Atomically saves caller-serialized JSON and keeps the previous version as .bak.</summary>
        public void Write(string json, string fileName = "settings.json")
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{", StringComparison.Ordinal) || !json.TrimEnd().EndsWith("}", StringComparison.Ordinal))
                throw new ArgumentException("Serialize a JSON object before saving.", nameof(json));
            SettingsFile.Write(PathFor(fileName), json);
        }

        /// <summary>Call after a read warning before replacing a damaged primary.</summary>
        public void PreserveInvalid(string fileName = "settings.json") { SettingsFile.PreserveInvalid(PathFor(fileName)); }

        private static void ValidateName(string value, string parameter)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 80 || !Regex.IsMatch(value, @"\A[A-Za-z0-9][A-Za-z0-9_.-]*\z") || value.EndsWith(".", StringComparison.Ordinal))
                throw new ArgumentException("Use 1–80 letters, digits, dots, underscores or hyphens, starting with a letter or digit.", parameter);
            string stem = value.Split('.')[0];
            if (Regex.IsMatch(stem, @"\A(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])\z", RegexOptions.IgnoreCase))
                throw new ArgumentException("Reserved system filename.", parameter);
        }
    }
}

