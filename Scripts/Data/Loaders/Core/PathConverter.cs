namespace PitsOfDespair.Data.Loaders.Core;

/// <summary>
/// Utility for converting file paths to data IDs.
/// </summary>
public static class PathConverter
{
    /// <summary>
    /// Converts a file path to a valid ID by extracting just the filename.
    /// Examples: "Goblins/warrior.yaml" -> "warrior"
    ///           "Vermin/rat.yaml" -> "rat"
    /// </summary>
    public static string ConvertToId(string path)
    {
        // Remove .yaml extension
        string withoutExtension = path.Replace(".yaml", "");

        // Extract just the filename (last part after /)
        string[] parts = withoutExtension.Split('/');
        string filename = parts[parts.Length - 1];

        // Convert to lowercase
        return filename.ToLower();
    }
}
