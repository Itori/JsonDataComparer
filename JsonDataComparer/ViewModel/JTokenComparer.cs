using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using JsonDataComparer.Localization;
using JsonDataComparer.Model;
using Newtonsoft.Json.Linq;

namespace JsonDataComparer.ViewModel
{
    public class JTokenComparer : IEqualityComparer<JToken>
    {
        private readonly Dictionary<string, List<ComparisonRule>> _rules;
        private readonly ObservableCollection<string> _logEntries;
        private readonly bool _ignoreNullValues;

        public JTokenComparer(Dictionary<string, List<ComparisonRule>> rules, ObservableCollection<string> logEntries, bool ignoreNullValues)
        {
            _rules = rules;
            _logEntries = logEntries;
            _ignoreNullValues = ignoreNullValues;
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
            _logEntries.Add(string.Format(message, args));
            //Console.WriteLine(message, args);
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

            //LogDiff(token1, token2);
            return false;
        }

        private bool Equals(JContainer container1, JContainer container2)
        {
            if (container1 == container2)
                return true;

            var t1 = container1?.Children().ToList();
            var t2 = container2?.Children().ToList();

            if (t1 == null)
            {
                Debug.Assert(t2 != null, nameof(t2) + " != null");
                if (t2.Count == 0)
                    return true;
                return false;
            }
            if (t2 == null)
            {
                Debug.Assert(t1 != null, nameof(t1) + " != null");
                if (t1.Count == 0)
                    return true;
                return false;
            }

            bool equals = true;
            for (int i = 0; i < t1.Count(); i++)
            {
                var token1 = t1.ElementAt(i);
                switch (token1)
                {
                    case JObject object1:
                        var path = Regex.Replace(token1.Path, "\\[[0-9]+\\]", "[]");
                        if(!_rules.TryGetValue(path, out var comparisonRules))
                        {
                            Log("No Key found for Path {0}", path);
                            return false;
                        }

                        var keys = new HashSet<string>(comparisonRules.Where(c => !c.Ignore).Select(c => c.Name).Distinct());
                        var properties1 = object1.Properties().Where(p => keys.Contains(p.Name)).ToList();
                        if (!properties1.Any())
                        {
                            Log($"Keys not found for Path {0}", path);
                            return false;
                        }

                        var object2 = t2.OfType<JObject>().FirstOrDefault(t =>
                        {
                            var properties2 = t.Properties().Where(p => keys.Contains(p.Name)).ToList();

                            foreach (var p1 in properties1)
                            {
                                var p2 = properties2.FirstOrDefault(p => p.Name == p1.Name);
                                if (p2 != null)
                                    properties2.Remove(p2);

                                if (!Equals(p1, p2))
                                    return false;
                            }

                            foreach (var p2 in properties2)
                            {
                                if (!Equals(null, p2))
                                    return false;
                            }

                            return true;
                        });

                        if (!Equals(object1, object2))
                        {
                            @equals = false;
                            LogDiff(object1, object2);
                        }

                        if (object2 != null)
                            t2.Remove(object2);

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

            if (t2.Count > 0)
            {
                foreach (var item in t2)
                {
                    switch (item)
                    {
                        case JProperty property:
                            if (Equals(null, property))
                                continue;
                            break;
                    }

                    equals = false;
                    LogDiff(null, item);
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

            foreach (var property in properties2)
            {                
                if (!Equals(null, property))
                {
                    equal = false;
                    LogDiff(null, property);
                }
            }


            return equal;
        }

        private bool IsIgnored(JToken token)
        {
            if (token == null)
                return false;
            var path = Regex.Replace(token.Path, "\\[[0-9]+\\]", "[]");

            var indexDot = path.LastIndexOf('.');
            var xpath = path.Substring(0, indexDot);
            var key = path.Substring(indexDot + 1);

            if (_rules.ContainsKey(xpath))
            {
                return _rules[xpath].Where(r => r.Ignore).Any(r => r.Name == key);
            }

            return false;
        }

        private bool Equals(JValue value1, JValue value2)
        {
            if (IsIgnored(value1) || IsIgnored(value2))
                return true;

            if (value1 != null && value2 != null)
                return value1.Equals(value2);
            if (value1 == null && value2 != null)
            {
                if (value2.Value == null && _ignoreNullValues)
                    return true;
            } else if (value1 != null)
            {
                if (value1.Value == null && _ignoreNullValues)
                    return true;
            }
            else
                return true;

            return false;
        }

        private bool Equals(JProperty property1, JProperty property2)
        {
            if (property1 != null)
            {
                switch (property1.Value)
                {
                    case JValue value1 when property2.Value is JValue value2:
                        if (Equals(value1, value2))
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
            }
            else
            {
                switch (property2.Value)
                {
                    case JValue value2:
                        return Equals(null, value2);
                    case JContainer container2:
                        return Equals(null, container2);
                }
            }

            return false;
        }

        public int GetHashCode(JToken obj)
        {
            throw new NotImplementedException();
        }
    }
}