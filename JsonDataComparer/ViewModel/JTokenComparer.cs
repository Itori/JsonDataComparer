using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JsonDataComparer.Localization;
using JsonDataComparer.Model;
using Newtonsoft.Json.Linq;

namespace JsonDataComparer.ViewModel
{
    public class JTokenComparer : IEqualityComparer<JToken>
    {
        private readonly Dictionary<string, string> _rules;

        public JTokenComparer(Dictionary<string, string> rules)
        {
            _rules = rules;
        }

        private void LogDiff(JToken token1, JToken token2)
        {
            if (token1 != null && token2 == null)
                Log(Strings.LabelNotFoundFile2, token1.Path);
            else if (token1 == null && token2 != null)
                Log(Strings.LabelNotFoundFile1, token2.Path);
            else
                Log(Strings.LabelDifferentMatch, token1.Path, token2.Path);
        }

        private void Log(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        public bool Equals(JToken token1, JToken token2)
        {
            if (token1 == null) throw new ArgumentNullException(nameof(token1));
            if (token2 == null) throw new ArgumentNullException(nameof(token2));

            switch (token1)
            {
                case JValue value1:
                    if (token2 is JValue value2 && value1.Equals(value2))
                        return true;
                    break;
                case JObject object1:
                    if (token2 is JObject object2 && Equals(object1, object2)) return true;
                    break;
                case JContainer container1:
                    if (token2 is JContainer container2 && Equals(container1, container2))
                        return true;
                    break;
                default:
                    throw new NotImplementedException($"Type not implemented : {token1.GetType().FullName}");
            }

            LogDiff(token1, token2);
            return false;
        }

        private bool Equals(JContainer container1, JContainer container2)
        {
            if (container1 == container2)
                return true;

            var t1 = container1.Children().ToList();
            var t2 = container2.Children().ToList();

            bool equals = true;
            for (int i = 0; i < t1.Count(); i++)
            {
                var token1 = t1.ElementAt(i);
                switch (token1)
                {
                    case JObject object1:
                        var path = Regex.Replace(token1.Path, "\\[[0-9]+\\]", "[]");
                        if(!_rules.TryGetValue(path, out var key))
                        {
                            Log("No Key found for Path {0}", path);
                            return false;
                        }

                        var id = ((JValue)object1.Property(key).Value).Value.ToString();

                        var object2 = t2.OfType<JObject>().FirstOrDefault(t =>
                        {
                            if (t.TryGetValue(key, out JToken id2))
                            {
                                if (((JValue)id2).Value.ToString() == id)
                                    return true;
                            }

                            return false;
                        });

                        if (!Equals(object1, object2))
                        {
                            @equals = false;
                            LogDiff(object1, object2);
                        }

                        break;

                    case JProperty property1:
                        var property2 = t2.OfType<JProperty>().FirstOrDefault(p => p.Name == property1.Name);
                        if (property2 != null)
                        {
                            t2.Remove(property2);

                            if (Equals(property1, property2))
                                continue;
                        }

                        equals = false;
                        LogDiff(property1, property2);

                        break;
                    default:
                        throw new NotImplementedException("Compare collections of something else then JObject");
                }
            }

            return equals;
        }

        private bool Equals(JObject object1, JObject object2)
        {
            if (object1 == null ^ object2 == null)
                return false;
            if (object1 == object2)
                return true;

            var properties1 = object1.Properties().OrderBy(r => r.Value.Type == JTokenType.Array).ToList();
            var properties2 = object2.Properties().OrderBy(r => r.Value.Type == JTokenType.Array).ToList();

            bool equal = true;
            foreach (var property1 in properties1)
            {
                var property2 = properties2.FirstOrDefault(p => p.Name == property1.Name);
                if (property2 != null)
                {
                    properties2.Remove(property2);

                    if (Equals(property1, property2))
                        continue;
                }

                equal = false;
                LogDiff(property1, property2);
            }


            return equal;
        }

        private bool Equals(JProperty property1, JProperty property2)
        {
            switch (property1.Value)
            {
                case JValue value1 when property2.Value is JValue value2:
                    if (value1.Equals(value2))
                        return true;
                    break;
                case JContainer container1 when property2.Value is JContainer container2:
                    if (Equals(container1, container2))
                        return true;

                    break;

                //case JObject childObject1 when property2.Value is JObject childObject2:
                //    if (Equals(childObject1, childObject2))
                //        continue;

                default:
                    break;
            }

            return false;
        }

        public int GetHashCode(JToken obj)
        {
            throw new NotImplementedException();
        }
    }
}