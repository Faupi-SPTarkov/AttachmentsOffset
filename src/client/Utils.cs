using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace AttachmentsOffset.Utils
{
    public class Vector3Json
    {
        public float x;
        public float y;
        public float z;

        [JsonConstructor]
        public Vector3Json()
        {
        }

        public Vector3Json(float _x, float _y, float _z)
        {
            this.x = _x;
            this.y = _y;
            this.z = _z;
        }

        public Vector3Json(Vector3 vec)
        {
            this.x = vec.x;
            this.y = vec.y;
            this.z = vec.z;
        }
    }

    public static class Vector3Json_Tools
    {
        public static Vector3Json ToJsonVector(this Vector3 vec)
        {
            return new Vector3Json(vec);
        }

        public static Vector3 FromJsonVector(this Vector3Json vec)
        {
            return new Vector3(vec.x, vec.y, vec.z);
        }
    }

    public static class Component_Tools
    {
        public static void CopyComponentFields<TTarget, TOriginal>(this TTarget target, TOriginal original) where TOriginal : Component where TTarget : Component
        {
            FieldInfo[] originalFields = typeof(TOriginal).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            //Debug.LogError($"Cloning {original.GetType()} to {target.GetType()}:");
            foreach (FieldInfo originalField in originalFields)
            {
                var value = originalField.GetValue(original);

                FieldInfo[] targetFields = typeof(TTarget).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                // Filter to the closest matching field, needs to have "same" type and name
                try
                {
                    FieldInfo targetField = targetFields?.Where(_targetField => _targetField.Name == originalField.Name && (_targetField.FieldType == originalField.FieldType || _targetField.FieldType.IsAssignableFrom(originalField.FieldType) || originalField.FieldType.IsAssignableFrom(_targetField.FieldType)))?.First();
                    targetField.SetValue(target, value);
                    //Debug.LogError($"  {originalField.Name}: {value}");
                }
                catch{}
            }
        }
    }

    public static class String_Tools
    {
        public static string ToSentenceCase(this string text)
        {
            string result = text;
            try
            {
                // start by converting entire string to lower case
                var lowerCase = text.ToLower();
                // matches the first sentence of a string, as well as subsequent sentences
                var r = new Regex(@"(^[a-z])|\.\s+(.)", RegexOptions.ExplicitCapture);
                // MatchEvaluator delegate defines replacement of setence starts to uppercase
                result = r.Replace(lowerCase, s => s.Value.ToUpper());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting string case for '{text}': {ex}");
            }

            return result;
        }
    }

    public static class JSON_Tools
    {
        public static bool IsValidJson(this string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
