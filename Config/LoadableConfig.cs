using JetBrains.Annotations;
using System;
using System.Linq;
using System.Reflection;

namespace Riey.Common.Config
{
    public abstract class LoadableConfig
    {
        private readonly FieldInfo[] _loadableFields;
        private readonly PropertyInfo[] _loadableProperties;

        protected LoadableConfig()
        {
            _loadableFields =
                GetType().GetRuntimeFields().Where(field => field.IsDefined(typeof(LoadableFieldAttribute))).ToArray();
            _loadableProperties =
                GetType()
                    .GetRuntimeProperties()
                    .Where(property => property.IsDefined(typeof(LoadablePropertyAttribute)))
                    .ToArray();
        }

        protected virtual void AddParsers(ConfigDic configDic)
        {
        }

        protected virtual void AddWriters(ConfigDic configDic)
        {
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
        protected abstract class ConfigItemAttribute : Attribute
        {
            [CanBeNull]
            public string Tag { get; set; }

            [CanBeNull]
            public string Key { get; set; }

            public string DefaultValue { get; }

            protected ConfigItemAttribute(string defaultValue)
            {
                DefaultValue = defaultValue;
            }
        }

        [AttributeUsage(AttributeTargets.Property)]
        protected class LoadablePropertyAttribute : ConfigItemAttribute
        {
            public LoadablePropertyAttribute(string defaultValue) : base(defaultValue)
            {
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        protected class LoadableFieldAttribute : ConfigItemAttribute
        {
            public LoadableFieldAttribute(string defaultValue) : base(defaultValue)
            {
            }
        }

        protected void Load(ConfigDic configDic)
        {
            AddParsers(configDic);
            MethodInfo getValue = typeof(ConfigDic)
                .GetRuntimeMethod("GetValue", new[] {typeof(string), typeof(string)});
            foreach (var field in _loadableFields)
            {
                var attr = field.GetCustomAttribute<LoadableFieldAttribute>();
                string tag = attr.Tag ?? ConfigDic.DefaultTag;
                string key = attr.Key ?? field.Name;
                string defaultValue = attr.DefaultValue;

                if (!configDic.HasTag(tag))
                    configDic.AddTag(tag);
                if (!configDic.HasKey(tag, key))
                    configDic.SetValue(tag, key, defaultValue);

                field.SetValue(
                               this,
                               getValue.MakeGenericMethod(field.FieldType).Invoke(configDic, new object[] {tag, key}));
            }

            foreach (var property in _loadableProperties)
            {
                var attr = property.GetCustomAttribute<LoadablePropertyAttribute>();
                string tag = attr.Tag ?? ConfigDic.DefaultTag;
                string key = attr.Key ?? property.Name;
                string defaultValue = attr.DefaultValue;

                if (!configDic.HasTag(tag))
                    configDic.AddTag(tag);
                if (!configDic.HasKey(tag, key))
                    configDic.SetValue(tag, key, defaultValue);

                property.SetValue(
                                  this,
                                  getValue.MakeGenericMethod(property.PropertyType)
                                          .Invoke(configDic, new object[] {tag, key}));
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
                                          select m).First()
                                                   .MakeGenericMethod(field.FieldType);

                var attr = field.GetCustomAttribute<LoadableFieldAttribute>();
                string tag = attr.Tag ?? ConfigDic.DefaultTag;
                string key = attr.Key ?? field.Name;

                setValue.Invoke(configDic, new object[] {tag, key, field.GetValue(this)});
            }

            foreach (var property in _loadableProperties)
            {
                MethodInfo setValue = (
                                          from m in typeof(ConfigDic).GetRuntimeMethods()
                                          where m.Name == "SetValue" && m.IsGenericMethodDefinition
                                          let parameter = m.GetParameters()
                                          where parameter.Length == 3
                                          select m).First()
                                                   .MakeGenericMethod(property.PropertyType);

                var attr = property.GetCustomAttribute<LoadablePropertyAttribute>();
                string tag = attr.Tag ?? ConfigDic.DefaultTag;
                string key = attr.Key ?? property.Name;

                setValue.Invoke(configDic, new[] {tag, key, property.GetValue(this)});
            }
        }
    }
}
