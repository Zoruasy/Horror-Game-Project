using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class UnityTypesConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        Type type = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4)
            || type == typeof(Vector2Int) || type == typeof(Vector3Int)
            || type == typeof(Quaternion) || type == typeof(Color);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        float[] components = value switch
        {
            Vector2 v => new[] { v.x, v.y },
            Vector3 v => new[] { v.x, v.y, v.z },
            Vector4 v => new[] { v.x, v.y, v.z, v.w },
            Vector2Int v => new float[] { v.x, v.y },
            Vector3Int v => new float[] { v.x, v.y, v.z },
            Quaternion q => new[] { q.x, q.y, q.z, q.w },
            Color c => new[] { c.r, c.g, c.b, c.a },
            _ => Array.Empty<float>()
        };

        writer.WriteStartArray();
        foreach (float component in components) writer.WriteValue(component);
        writer.WriteEndArray();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return existingValue;

        JArray array = JArray.Load(reader);
        float At(int i, float fallback = 0f) => i < array.Count ? array[i].Value<float>() : fallback;

        Type type = Nullable.GetUnderlyingType(objectType) ?? objectType;
        if (type == typeof(Vector2)) return new Vector2(At(0), At(1));
        if (type == typeof(Vector3)) return new Vector3(At(0), At(1), At(2));
        if (type == typeof(Vector4)) return new Vector4(At(0), At(1), At(2), At(3));
        if (type == typeof(Vector2Int)) return new Vector2Int(Mathf.RoundToInt(At(0)), Mathf.RoundToInt(At(1)));
        if (type == typeof(Vector3Int)) return new Vector3Int(Mathf.RoundToInt(At(0)), Mathf.RoundToInt(At(1)), Mathf.RoundToInt(At(2)));
        if (type == typeof(Quaternion)) return array.Count < 4 ? Quaternion.identity : new Quaternion(At(0), At(1), At(2), At(3));
        if (type == typeof(Color)) return new Color(At(0), At(1), At(2), At(3, 1f));
        return existingValue;
    }
}
