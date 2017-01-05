using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YeongHun.Common.Config.BuiltInMethod
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

        [Parser(typeof(Int64))]
        public static object Int64Parser(string rawStr) => Int64.Parse(rawStr);

        [Parser(typeof(Int32))]
        public static object Int32Parser(string rawStr) => Int32.Parse(rawStr);

        [Parser(typeof(Int16))]
        public static object Int16Parser(string rawStr) => Int16.Parse(rawStr);

        [Parser(typeof(Byte))]
        public static object ByteParser(string rawStr) => Byte.Parse(rawStr);

        [Parser(typeof(Boolean))]
        public static object BooleanParser(string rawStr) => Boolean.Parse(rawStr);

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
