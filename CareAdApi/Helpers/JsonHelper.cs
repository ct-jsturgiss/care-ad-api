using System.Reflection;
using System.Text.Json.Serialization;

namespace CareAdApi.Helpers
{
    public static class JsonHelper
    {
        public static string[] GetSerializedKeys<T>()
        {
            List<string> keys = new List<string>();

            Type objType = typeof(T);
            PropertyInfo[] props = objType.GetProperties();
            foreach(PropertyInfo prop in props)
            {
                JsonIgnoreAttribute? attrIgn = prop.GetCustomAttribute<JsonIgnoreAttribute>();
                if(attrIgn != null) { continue; }

                JsonPropertyNameAttribute? attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                if(attr != null)
                {
                    keys.Add(attr.Name);
                }
                else
                {
                    keys.Add(prop.Name);
                }
            }

            return keys.ToArray();
        }
    }
}
