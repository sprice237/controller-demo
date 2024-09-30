using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic;

namespace ControllerDemo.Common
{
    public static class JsonExtensions
    {
        public static T DeserializeJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            });
        }

        public static object DeserializeJson(this string json, Type type)
        {
            return typeof(JsonSerializer)
                .GetMethod("Deserialize", new[] { typeof(string), typeof(JsonSerializerOptions) })?
                .MakeGenericMethod(type)
                .Invoke(null, new object[] {
                    json,
                    new JsonSerializerOptions
                    {
                        Converters =
                        {
                            new JsonStringEnumConverter()
                        }
                    }
                });
        }

        public static string SerializeJson<T>(this T o)
        {
            return JsonSerializer.Serialize(o, new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            });
        }
    }
}