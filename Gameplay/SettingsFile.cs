using System;
using System.IO;
using System.Text;

namespace UmbraMenu
{
    /// <summary>Unity-independent, atomic UTF-8 settings storage with a last-known-good backup.</summary>
    internal static class SettingsFile
    {
        /// <summary>Tries the primary and backup; the caller validates a fresh candidate before accepting it.</summary>
        public static bool Read(string path, Action<string> accept, out string warning, string legacyPath = null)
        {
            if (accept == null) throw new ArgumentNullException(nameof(accept));
            if (!File.Exists(path) && !File.Exists(path + ".bak") && legacyPath != null)
            {
                string accepted = null;
                bool imported = Read(legacyPath, json => { accept(json); accepted = json; }, out warning);
                if (!imported) return false;
                try { Write(path, accepted); }
                catch (Exception error) { warning = "Loaded legacy settings, but migration could not be saved: " + error.Message; }
                return true;
            }
            warning = null;
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    string text = File.ReadAllText(candidate, Encoding.UTF8);
                    if (string.IsNullOrWhiteSpace(text) || !text.TrimStart().StartsWith("{", StringComparison.Ordinal) || !text.TrimEnd().EndsWith("}", StringComparison.Ordinal))
                        throw new InvalidDataException("Expected a complete JSON object.");
                    accept(text);
                    if (candidate != path) warning = "Recovered settings from backup: " + System.IO.Path.GetFileName(path);
                    return true;
                }
                catch (Exception error) { warning = System.IO.Path.GetFileName(candidate) + ": " + error.Message; }
            }
            return false;
        }

        /// <summary>Flushes a temporary sibling, then atomically replaces the destination while preserving its previous version.</summary>
        public static void Write(string path, string json)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }

        /// <summary>Preserves an unreadable/recovered primary before any later save replaces it or its backup.</summary>
        public static void PreserveInvalid(string path)
        {
            if (File.Exists(path)) File.Copy(path, path + ".invalid-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
        }
    }
}


