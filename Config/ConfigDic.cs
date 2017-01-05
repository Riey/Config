using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;

namespace YeongHun.Common.Config
{
    public class ConfigDic
    {

        private class DictionaryFactory
        {
            private bool _sorted;

            public DictionaryFactory(bool sorted)
            {
                _sorted = sorted;
            }

            public IDictionary<TKey, TValue> Create<TKey, TValue>() =>
                _sorted ? new SortedDictionary<TKey, TValue>() as IDictionary<TKey,TValue> : new Dictionary<TKey, TValue>();
        }


        internal static readonly string DefaultTag = "Common";
        private Dictionary<Type, ConfigParser<object>> _parserDic;
        private Dictionary<Type, ConfigWriter<object>> _writerDic = new Dictionary<Type, ConfigWriter<object>>();
        private static readonly Regex configPattern = new Regex(@"^\s*(?<ConfigName>([^\s]|(\s+[^=]))+)\s*=\s*(?<ConfigValue>.*)$");
        private static readonly Regex configTagPattern = new Regex(@"\[(?<TagName>.*)\]");

        private IDictionary<string, IDictionary<string, string>> _rawValues;

        private DictionaryFactory _tagDicFactory, _keyDicFactory;

        /// <summary>
        /// 문자열을 지정된 형식으로 변환합니다
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rawStr"></param>
        /// <returns></returns>
        public delegate T ConfigParser<T>(string rawStr);
        /// <summary>
        /// 지정된 형식을 문자열로 변환합니다
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public delegate string ConfigWriter<T>(T value);

        public ConfigDic() : this(false, false) { }

        public ConfigDic(bool sortTag, bool sortKey)
        {
            _parserDic = BuiltInMethod.Parser.GetBuiltInParsers();

            _tagDicFactory = new DictionaryFactory(sortTag);
            _keyDicFactory = new DictionaryFactory(sortKey);

            _rawValues = _tagDicFactory.Create<string, IDictionary<string, string>>();
        }

        public string this[string key]
        {
            get
            {
                return _rawValues[DefaultTag][key];
            }
            set
            {
                _rawValues[DefaultTag][key] = value;
            }
        }

        public string this[string tag, string key]
        {
            get
            {
                return _rawValues[tag][key];
            }
            set
            {
                _rawValues[tag][key] = value;
            }
        }

        public bool HasTag(string tag) => _rawValues.ContainsKey(tag);
        public void AddTag(string tag)
        {
            if (!_rawValues.ContainsKey(tag))
                _rawValues.Add(tag, _keyDicFactory.Create<string, string>());
        }

        public void AddParser<T>(ConfigParser<T> parser)
        {
            if (!_parserDic.ContainsKey(typeof(T)))
                _parserDic.Add(typeof(T), str => parser(str));
            else
                _parserDic[typeof(T)] = str => parser(str);
        }

        public void AddWriter<T>(ConfigWriter<T> writer)
        {
            if (!_writerDic.ContainsKey(typeof(T)))
                _writerDic.Add(typeof(T), value => writer((T)value));
            else
                _writerDic[typeof(T)] = value => writer((T)value);
        }


        public void SetValue<T>(string key, T value) => SetValue(DefaultTag, key, value);
        public void SetValue<T>(string tag, string key, T value)
        {
            if (!_rawValues.ContainsKey(tag))
                throw new ArgumentException("존재하지 않는 태그이름입니다 tag : " + tag);

            string str = _writerDic.ContainsKey(typeof(T)) ? _writerDic[typeof(T)](value) : value.ToString();

            if (!_rawValues[tag].ContainsKey(key))
                _rawValues[tag].Add(key, str);
            else
                _rawValues[tag][key] = str;
        }

        public bool HasKey(string key) => HasKey(DefaultTag, key);
        public bool HasKey(string tag, string key) => _rawValues[tag].ContainsKey(key);

        public T GetValue<T>(string key) => GetValue<T>(DefaultTag, key);
        public T GetValue<T>(string tag, string key)
        {
            if (!_rawValues.ContainsKey(tag))
                throw new ArgumentException("존재하지 않는 태그이름입니다 tag : " + tag);
            if (!_rawValues[tag].ContainsKey(key))
                throw new ArgumentException("존재하지 않는 설정 이름입니다 key : " + key);
            if (_parserDic.TryGetValue(typeof(T), out var parser))
                try
                {
                    return (T)parser(_rawValues[tag][key]);
                }
                catch(InvalidCastException e)
                {
                    throw new ArgumentException($"Tag : {tag} Key : {key} Value : {_rawValues[tag][key]}를 파싱하는중 오류가 발생했습니다", e);
                }
            else if (TryParse(_rawValues[tag][key], out T value))
                return value;
            else
                throw new InvalidCastException("지정된 타입으로 변환 할 수 없습니다");
        }

        public bool TryGetValue<T>(string key, out T value) => TryGetValue(DefaultTag, key, out value);
        public bool TryGetValue<T>(string tag, string key, out T value) => TryGetValue(tag, key, out value, null, default(T));

        public bool TryGetValue<T>(string key, out T value, string defaultString) => TryGetValue(DefaultTag, key, out value, defaultString);
        public bool TryGetValue<T>(string tag, string key, out T value, string defaultString)
        {
            if (!_rawValues[tag].ContainsKey(key))
            {
                if (defaultString != null)
                    SetValue(tag, key, defaultString);
                value = GetValue<T>(tag, key);
                return false;
            }
            else if (!_parserDic.TryGetValue(typeof(T), out var parser))
            {
                return TryParse(_rawValues[tag][key], out value);
            }
            else
            {
                try
                {
                    value = (T)parser(_rawValues[tag][key]);
                    return true;
                }
                catch (InvalidCastException e)
                {
                    throw new ArgumentException($"Tag : {tag} Key : {key} Value : {_rawValues[tag][key]}를 파싱하는중 오류가 발생했습니다", e);
                }
            }
        }

        private bool TryParse<T>(string str, out T value)
        {
            value = default(T);
            if(value is Enum)
            {
                try
                {
                    value = (T)Enum.Parse(typeof(T), str, true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            var parse = typeof(T).GetRuntimeMethods()
                .Where(
                method => method.Name == "Parse" &&
                method.Attributes.HasFlag(MethodAttributes.Static)&&
                method.ReturnType == typeof(T)).FirstOrDefault();

            if (parse == null)
                return false;

            var parameters = parse.GetParameters();
            if (parameters.Length != 1 && parameters[0].ParameterType != typeof(string))
                return false;
            try
            {
                value = (T)parse.Invoke(null, new object[] { str });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetValue<T>(string key, out T value, string defaultString, T defaultValue) => TryGetValue(DefaultTag, key, out value, defaultString, defaultValue);
        public bool TryGetValue<T>(string tag, string key, out T value, string defaultString, T defaultValue)
        {
            value = defaultValue;
            bool ReturnDefault()
            {
                if (defaultString != null)
                    SetValue(tag, key, defaultString);
                return false;
            }
            if (!_rawValues[tag].ContainsKey(key))
            {
                return ReturnDefault();
            }
            else if (!_parserDic.TryGetValue(typeof(T), out var parser))
            {
                if (TryParse(_rawValues[tag][key], out value))
                    return true;
                else
                    return ReturnDefault();
            }
            else
            {
                value = (T)parser(_rawValues[tag][key]);
                return true;
            }
        }

        public void Save(Stream stream, bool dispose = true) => Save(new StreamWriter(stream), dispose);

        public void Save(TextWriter output, bool dispose = true)
        {
            foreach (var value in _rawValues)
            {
                output.WriteLine("[" + value.Key + "]");
                output.WriteLine();
                foreach (var item in value.Value)
                {
                    output.WriteLine(item.Key + " = " + item.Value);
                }
                output.WriteLine();
                output.WriteLine();
            }
            output.Flush();
            if (dispose)
                output.Dispose();
        }

        public void Load(Stream stream, Encoding encoding = null, bool dispose = true) => Load(new StreamReader(stream, encoding ?? Encoding.UTF8, true), dispose);

        public void Load(TextReader reader, bool dispose = true)
        {
            _rawValues.Clear();
            string currentTag = DefaultTag;
            _rawValues.Add(currentTag, _keyDicFactory.Create<string, string>());
            while (reader.Peek() != -1)
            {
                var line = reader.ReadLine();
                var match = configPattern.Match(line);
                if (!match.Success)
                {
                    match = configTagPattern.Match(line);
                    if (match.Success)
                    {
                        currentTag = match.Groups["TagName"].Value;
                        if(!_rawValues.ContainsKey(currentTag))
                            _rawValues.Add(currentTag, _keyDicFactory.Create<string, string>());
                    }
                    continue;
                }
                _rawValues[currentTag].Add(match.Groups["ConfigName"].Value, match.Groups["ConfigValue"].Value);
            }

            if (dispose)
                reader.Dispose();
        }
    }
}
