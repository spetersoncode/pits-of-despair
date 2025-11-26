using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using PitsOfDespair.Core;

namespace PitsOfDespair.Data.Loaders.Core;

/// <summary>
/// Core YAML loading utility for all data loaders.
/// Provides consistent deserialization with custom type converters.
/// </summary>
public static class YamlLoader
{
    /// <summary>
    /// Loads and deserializes a YAML file.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="path">The resource path to the YAML file.</param>
    /// <returns>The deserialized object, or null if loading fails.</returns>
    public static T LoadFile<T>(string path) where T : class
    {
        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[YamlLoader] Failed to open file {path}");
                return null;
            }

            string yamlText = file.GetAsText();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new PaletteColorConverter())
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<T>(yamlText);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[YamlLoader] Error loading {path}: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// YamlDotNet type converter that allows YAML files to reference Palette colors by name.
/// Supports both formats: "color: Palette.Iron" and "color: "#665544"".
/// </summary>
public class PaletteColorConverter : IYamlTypeConverter
{
    private static readonly Dictionary<string, string> _colorCache = new();

    static PaletteColorConverter()
    {
        // Build cache of Palette color names to hex values using reflection
        var paletteType = typeof(Palette);
        var colorFields = paletteType.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in colorFields)
        {
            if (field.FieldType == typeof(Color))
            {
                var color = (Color)field.GetValue(null);
                var hexValue = Palette.ToHex(color);
                _colorCache[field.Name.ToLower()] = hexValue;
            }
        }
    }

    public bool Accepts(Type type)
    {
        return type == typeof(string);
    }

    private const string PalettePrefix = "Palette.";

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var value = scalar.Value;

        // If it's already a hex color (starts with #), return as-is
        if (value.StartsWith('#'))
        {
            return value;
        }

        // Only convert strings that explicitly request palette colors via "Palette." prefix
        if (value.StartsWith(PalettePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var colorName = value.Substring(PalettePrefix.Length).ToLower();
            if (_colorCache.TryGetValue(colorName, out var hexValue))
            {
                return hexValue;
            }
            GD.PrintErr($"[PaletteColorConverter] Unknown palette color 'Palette.{colorName}' - this will cause rendering failures!");
            return "#FF00FF"; // Return magenta as obvious error indicator
        }

        // Return non-prefixed strings unchanged
        return value;
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar((string)value));
    }
}
