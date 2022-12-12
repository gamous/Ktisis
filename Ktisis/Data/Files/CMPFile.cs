using ImGuiNET;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ktisis.Structs.Poses;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace Ktisis.Data.Files
{
    public class CMPFile // Not a serialize class, just a utility class to upgrade from cmp
    {
        public static PoseFile Upgrade(string jsonIn)
        {
            if (jsonIn == null)
            {
                return null;
            }

            PoseFile file = new PoseFile();

            // Dummy when reading
            file.Position = new Vector3(0, 0, 0);
            file.Rotation = new Quaternion(0, 0, 0, 0);
            file.Scale = new Vector3(0, 0, 0);

            // Cuz we are making new instance rather than deserialize from json
            file.Bones = new PoseContainer();

            // Deserialize as Dict
            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonIn);

            if (deserialized == null)
                return null;

            // Is pure mess
            foreach(KeyValuePair<string, string> pair in deserialized)
            {
                string key = pair.Key;

                // Filter invalid shit, not fast but good enough
                if (key.Equals("Description") || key.Equals("DateCreated") || key.Equals("CMPVersion") || key.Equals("Race") || key.Equals("Clan") || key.Equals("Body"))
                    continue;
                
                string value = pair.Value;

                // Skipping empty value
                if (value == null || value.Equals("null"))
                    continue;
                
                // The bone ends with "Size"
                if (key.EndsWith("Size"))
                {
                    string cleanedName = key.Replace("Size", "");
                    // In-case some rot before scale
                    if (file.Bones.ContainsKey(cleanedName))
                    {
                        file.Bones[cleanedName].Scale = GetScale(value);
                    }
                    else
                    {
                        Transform transform = new();
                        transform.Scale = GetScale(value);

                        file.Bones.Add(cleanedName, transform);
                    }
                }
                else // else is rotation (dont know why it doesnt have pos)
                {
                    // Pretty much the same situation as above
                    if (file.Bones.ContainsKey(key))
                    {
                        file.Bones[value].Rotation = GetRot(value);
                    }
                    else
                    {
                        Transform transform = new();
                        transform.Rotation = GetRot(value);

                        file.Bones.Add(key, transform);
                    }
                }
            }

            return file;
        }

        private static Vector3 GetScale(string valIn)
        {
            Vector3 scale = default;

            byte[] data = StringToByteArray(valIn);

            scale.X = BitConverter.ToSingle(data, 0);
            scale.Y = BitConverter.ToSingle(data, 4);
            scale.Z = BitConverter.ToSingle(data, 8);

            return scale;
        }

        private static Quaternion GetRot(string valIn)
        {
            Quaternion rot = default;

            byte[] data = StringToByteArray(valIn);

            rot.X = BitConverter.ToSingle(data, 0);
            rot.Y = BitConverter.ToSingle(data, 4);
            rot.Z = BitConverter.ToSingle(data, 8);
            rot.W = BitConverter.ToSingle(data, 12);

            return rot;
        }

        private static byte[] StringToByteArray(string hex)
        {
            try
            {
                hex = hex.Trim();
                string[] parts = hex.Split(' ');
                byte[] data = new byte[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    data[i] = byte.Parse(parts[i], NumberStyles.HexNumber);
                }

                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse string: {hex} to byte array", ex);
            }
        }
    }
}
