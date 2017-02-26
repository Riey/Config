﻿using System;
using System.Linq;
using System.Reflection;

namespace YeongHun.Common.Config
{
    public abstract class LoadableConfig
    {
        private FieldInfo[] _loadableFields;
        private PropertyInfo[] _loadableProperties;

        public LoadableConfig()
        {
            _loadableFields = GetType().GetRuntimeFields().Where(field => field.IsDefined(typeof(LoadableFieldAttribute))).ToArray();
            _loadableProperties = GetType().GetRuntimeProperties().Where(property => property.IsDefined(typeof(LoadablePropertyAttribute))).ToArray();
        }

        protected virtual void AddParsers(ConfigDic configDic) { }
        protected virtual void AddWriters(ConfigDic configDic) { }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
        public abstract class ConfigItemAttribute : Attribute
        {
            public string Tag { get; set; }
            public string Key { get; set; }
            public string DefaultValue { get; }
            public ConfigItemAttribute(string defaultValue) => DefaultValue = defaultValue;
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class LoadablePropertyAttribute:ConfigItemAttribute
        {
            public LoadablePropertyAttribute(string defaultValue) : base(defaultValue) { }
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class LoadableFieldAttribute : ConfigItemAttribute
        {
            public LoadableFieldAttribute(string defaultValue) : base(defaultValue) { }
        }

        protected void Load(ConfigDic configDic)
        {
            AddParsers(configDic);
            MethodInfo getValue = typeof(ConfigDic)
                .GetRuntimeMethod("GetValue", new[] { typeof(string), typeof(string) });
            foreach(var field in _loadableFields)
            {
                var attr = field.GetCustomAttribute<LoadableFieldAttribute>();
                var tag = attr.Tag ?? ConfigDic.DefaultTag;
                var key = attr.Key ?? field.Name;
                var defaultValue = attr.DefaultValue;

                if (!configDic.HasTag(tag))
                    configDic.AddTag(tag);
                if (!configDic.HasKey(tag, key))
                    configDic.SetValue(tag, key, defaultValue);

                field.SetValue(this, getValue.MakeGenericMethod(field.FieldType).Invoke(configDic, new object[] { tag, key }));
            }

            foreach (var property in _loadableProperties)
            {
                var attr = property.GetCustomAttribute<LoadablePropertyAttribute>();
                var tag = attr.Tag ?? ConfigDic.DefaultTag;
                var key = attr.Key ?? property.Name;
                var defaultValue = attr.DefaultValue;

                if (!configDic.HasTag(tag))
                    configDic.AddTag(tag);
                if (!configDic.HasKey(tag, key))
                    configDic.SetValue(tag, key, defaultValue);

                property.SetValue(this, getValue.MakeGenericMethod(property.PropertyType).Invoke(configDic, new object[] { tag, key }));
            }
        }

        protected void Save(ConfigDic configDic)
        {
            AddWriters(configDic);
            foreach (var field in _loadableFields)
            {
                MethodInfo setValue = (
                    from m in typeof(ConfigDic).GetRuntimeMethods()
                    where m.Name == "SetValue" && m.IsGenericMethodDefinition
                    let parameter = m.GetParameters()
                    where parameter.Length == 3
                    select m).First().MakeGenericMethod(field.FieldType);

                var attr = field.GetCustomAttribute<LoadableFieldAttribute>();
                var tag = attr.Tag ?? ConfigDic.DefaultTag;
                var key = attr.Key ?? field.Name;

                setValue.Invoke(configDic, new object[] { tag, key, field.GetValue(this) });
            }

            foreach(var property in _loadableProperties)
            {
                MethodInfo setValue = (
                    from m in typeof(ConfigDic).GetRuntimeMethods()
                    where m.Name == "SetValue" && m.IsGenericMethodDefinition
                    let parameter = m.GetParameters()
                    where parameter.Length == 3
                    select m).First().MakeGenericMethod(property.PropertyType);

                var attr = property.GetCustomAttribute<LoadablePropertyAttribute>();
                var tag = attr.Tag ?? ConfigDic.DefaultTag;
                var key = attr.Key ?? property.Name;

                setValue.Invoke(configDic, new object[] { tag, key, property.GetValue(this) });
            }
        }
    }
}
