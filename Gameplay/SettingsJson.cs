using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SimpleJSON;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Managed JSON for injected settings: explicitly includes palette lists and RGBA values without Unity's native type cache.</summary>
    internal static class SettingsJson
    {
        private static readonly FieldInfo[] fields = typeof(VisualPreferences).GetFields(BindingFlags.Instance | BindingFlags.Public);

        /// <summary>Writes every public preference field, including per-item and category styles.</summary>
        public static string Serialize(VisualPreferences preferences)
        {
            var result = new JSONObject();
            foreach (var field in fields) result[field.Name] = Encode(field.GetValue(preferences));
            return result.ToString(2);
        }

        /// <summary>Overlays known fields onto defaults so older files retain sensible values for new settings.</summary>
        public static void Populate(string json, VisualPreferences preferences)
        {
            var root = ParseObject(json);
            foreach (var field in fields)
            {
                var node = root[field.Name];
                if (node == null || node.IsNull) continue;
                field.SetValue(preferences, Decode(node, field.FieldType));
            }
        }

        /// <summary>Rejects empty/non-object documents before accepting a settings file.</summary>
        public static JSONNode ParseObject(string json)
        {
            var root = JSONNode.Parse(json);
            if (root == null || !root.IsObject || root.Count == 0) throw new InvalidDataException("Settings must contain a nonempty JSON object.");
            return root;
        }

        /// <summary>Encodes the deliberately small set of supported setting types; unsupported additions fail visibly.</summary>
        private static JSONNode Encode(object value)
        {
            if (value is bool) return (bool)value;
            if (value is int) return (int)value;
            if (value is float) return (float)value;
            if (value is string) return (string)value;
            if (value is Color)
            {
                var c = (Color)value;
                return new JSONObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
            }
            if (value is EspStyle)
            {
                var style = (EspStyle)value;
                return new JSONObject { ["Key"] = style.Key, ["Color"] = Encode(style.Color), ["Thickness"] = style.Thickness,
                    ["Enabled"] = style.Enabled, ["Boxes"] = style.Boxes, ["Labels"] = style.Labels };
            }
            if (value is IList)
            {
                var array = new JSONArray();
                foreach (var entry in (IList)value) if (entry != null) array.Add(Encode(entry));
                return array;
            }
            throw new InvalidDataException("Unsupported settings value: " + value?.GetType().Name);
        }

        /// <summary>Type-checks persisted values instead of silently converting malformed fields to zero or false.</summary>
        private static object Decode(JSONNode node, Type type)
        {
            if (type == typeof(bool) && node.IsBoolean) return node.AsBool;
            if (type == typeof(int) && node.IsNumber) return node.AsInt;
            if (type == typeof(float) && node.IsNumber) return node.AsFloat;
            if (type == typeof(Color) && node.IsObject)
                return new Color((float)Decode(node["r"], typeof(float)), (float)Decode(node["g"], typeof(float)),
                    (float)Decode(node["b"], typeof(float)), (float)Decode(node["a"], typeof(float)));
            if (type == typeof(List<EspStyle>) && node.IsArray)
            {
                var styles = new List<EspStyle>();
                foreach (var entry in node.Children)
                {
                    if (!entry.IsObject || !entry["Key"].IsString) throw new InvalidDataException("Invalid palette entry.");
                    var style = new EspStyle(entry["Key"].Value, new Color(0.28f, 0.66f, 1f, 1f));
                    if (entry["Color"] != null) style.Color = (Color)Decode(entry["Color"], typeof(Color));
                    if (entry["Thickness"] != null) style.Thickness = (float)Decode(entry["Thickness"], typeof(float));
                    if (entry["Enabled"] != null) style.Enabled = (bool)Decode(entry["Enabled"], typeof(bool));
                    if (entry["Boxes"] != null) style.Boxes = (bool)Decode(entry["Boxes"], typeof(bool));
                    if (entry["Labels"] != null) style.Labels = (bool)Decode(entry["Labels"], typeof(bool));
                    styles.Add(style);
                }
                return styles;
            }
            throw new InvalidDataException("Invalid settings value for " + type.Name + ".");
        }
    }
}
