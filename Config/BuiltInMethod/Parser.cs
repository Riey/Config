using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riey.Common.Config.BuiltInMethod
{
    static class Parser
    {
        private class ParserAttribute : Attribute
        {
            public Type ReturnType { get; }

            public ParserAttribute(Type returnType)
            {
                ReturnType = returnType;
            }
        }

        [Parser(typeof(string))]
        public static object StringParser(string rawStr) => rawStr;

        [Parser(typeof(long))]
        public static object Int64Parser(string rawStr) => long.Parse(rawStr);

        [Parser(typeof(int))]
        public static object Int32Parser(string rawStr) => int.Parse(rawStr);

        [Parser(typeof(short))]
        public static object Int16Parser(string rawStr) => short.Parse(rawStr);

        [Parser(typeof(byte))]
        public static object ByteParser(string rawStr) => byte.Parse(rawStr);

        [Parser(typeof(bool))]
        public static object BooleanParser(string rawStr) => bool.Parse(rawStr);

        public static Dictionary<Type, ConfigDic.ConfigParser<object>> GetBuiltInParsers()
        {
            return typeof(Parser).GetRuntimeMethods()
                .Where(method =>
                {
                    if (!method.IsDefined(typeof(ParserAttribute)))
                        return false;
                    var parameters = method.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                })
                .Select(method => method.CreateDelegate(typeof(ConfigDic.ConfigParser<object>)) as ConfigDic.ConfigParser<object>)
                .ToDictionary(parser => parser.GetMethodInfo().GetCustomAttribute<ParserAttribute>().ReturnType);
        }
    }
}
