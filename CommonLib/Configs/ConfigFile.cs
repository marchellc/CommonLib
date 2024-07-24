using CommonLib.Extensions;
using CommonLib.IO;
using CommonLib.Logging;
using CommonLib.Pooling.Pools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonLib.Configs
{
    public class ConfigFile
    {
        private readonly HashSet<Tuple<FieldInfo, string, string, object>> _configFields = new HashSet<Tuple<FieldInfo, string, string, object>>();
        private readonly HashSet<Tuple<PropertyInfo, string, string, object>> _configProps = new HashSet<Tuple<PropertyInfo, string, string, object>>();
        private readonly HashSet<Tuple<string, string, Action<object>, Func<object>, Type>> _configDynamic = new HashSet<Tuple<string, string, Action<object>, Func<object>, Type>>();

        public Func<string, Type, object> Deserializer { get; set; }
        public Func<object, string> Serializer { get; set; }

        public string Path { get; }

        public FileWatcher Watcher { get; }
        public LogOutput Log { get; set; }

        public bool IsWatched
        {
            get => Watcher.IsEnabled;
            set => Watcher.IsEnabled = value;
        }

        public bool IsValid
        {
            get => Deserializer != null && Serializer != null;
        }

        public event Action<Dictionary<string, string>> OnLoaded;

        public ConfigFile(string path)
        {
            Path = path;

            Watcher = new FileWatcher(path);
            Watcher.IsEnabled = false;

            Watcher.OnChanged += OnChanged;
        }

        public bool Bind<T>(string name, string description, Action<T> setter, Func<T> getter)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException(nameof(description));

            if (setter is null)
                throw new ArgumentNullException(nameof(setter));

            if (getter is null)
                throw new ArgumentNullException(nameof(getter));

            if (_configDynamic.Any(t => t.Item1 == name))
                return false;

            _configDynamic.Add(new Tuple<string, string, Action<object>, Func<object>, Type>(name, description, value => setter((T)value), () => getter(), typeof(T)));
            return true;
        }

        public bool Bind()
            => Bind(Assembly.GetCallingAssembly());

        public bool Bind(Assembly assembly)
        {
            var any = false;

            foreach (var type in assembly.GetTypes())
            {
                var result = Bind(type, null);

                if (!any && result)
                    any = true;
            }

            return any;
        }

        public bool Bind<T>(T instance = default)
            => Bind(typeof(T), instance);

        public bool Bind(Type type, object instance = null)
        {
            var count = 0;

            foreach (var field in type.GetAllFields())
            {
                if (!field.HasAttribute<ConfigAttribute>(out var configAttribute))
                    continue;

                if (field.IsInitOnly || (!field.IsStatic && !type.IsValidInstance(instance, false)))
                    continue;

                if (_configFields.Any(t => t.Item1 == field && t.Item2.IsEqualTo(instance)))
                    continue;

                if (_configDynamic.Any(t => t.Item1.ToLower() == field.Name.ToLower()))
                    continue;

                Log?.Debug($"Bound config field '{field.ToName()}'");

                _configFields.Add(new Tuple<FieldInfo, string, string, object>(field, configAttribute.Name, string.Join("\n", configAttribute.Description), instance));
                count++;
            }

            foreach (var prop in type.GetAllProperties())
            {
                if (!prop.CanWrite || !prop.CanRead)
                    continue;

                if (!prop.HasAttribute<ConfigAttribute>(out var configAttribute))
                    continue;

                var getMethod = prop.GetGetMethod(true);
                var setMethod = prop.GetSetMethod(true);

                if (setMethod is null || getMethod is null)
                    continue;

                if (!getMethod.IsStatic && !setMethod.IsStatic && !type.IsValidInstance(instance, false))
                    continue;

                if (_configProps.Any(t => t.Item1 == prop && t.Item2.IsEqualTo(instance)))
                    continue;

                if (_configDynamic.Any(t => t.Item1.ToLower() == prop.Name.ToLower()))
                    continue;

                Log?.Debug($"Bound config property '{prop.ToName()}'");

                _configProps.Add(new Tuple<PropertyInfo, string, string, object>(prop, configAttribute.Name, string.Join("\n", configAttribute.Description), instance));
                count++;
            }

            return count > 0;
        }

        public bool Unbind(Assembly assembly)
        {
            var count = 0;

            count += _configFields.RemoveWhere(t => t.Item1.DeclaringType != null && t.Item1.DeclaringType.Assembly == assembly);
            count += _configProps.RemoveWhere(t => t.Item1.DeclaringType != null && t.Item1.DeclaringType.Assembly == assembly);

            return count > 0;
        }

        public bool Unbind(Type type, object instance = null)
        {
            var count = 0;

            count += _configFields.RemoveWhere(t => t.Item1.DeclaringType != null && t.Item1.DeclaringType == type && t.Item2.IsEqualTo(instance));
            count += _configProps.RemoveWhere(t => t.Item1.DeclaringType != null && t.Item1.DeclaringType == type && t.Item2.IsEqualTo(instance));

            return count > 0;
        }

        public bool Unbind(string name)
            => _configDynamic.RemoveWhere(t => t.Item1.ToLower() == name.ToLower()) > 0;

        public void Clear()
        {
            _configDynamic.Clear();
            _configFields.Clear();
            _configProps.Clear();
        }

        public Dictionary<string, string> Load()
        {
            if (!IsValid)
                throw new InvalidOperationException($"This config file is missing a serializer/deserializer!");

            if (!System.IO.File.Exists(Path))
                return Save();

            var lines = System.IO.File.ReadAllLines(Path);

            if (lines is null || lines.Length < 2)
                return Save();

            var collected = DictionaryPool<string, string>.Shared.Rent();
            var failed = new Dictionary<string, string>();

            var str = "";
            var key = "";

            foreach (var lineStr in lines)
            {
                var line = lineStr.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]") && line != "[]")
                {
                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(str))
                        collected[key] = str;

                    key = line.Remove("[", "]").Trim();
                    str = "";

                    continue;
                }

                str += $"\n{line}";
            }

            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(str))
                collected[key] = str;

            foreach (var field in _configFields)
            {
                if (!collected.TryGetValue(field.Item2, out var fieldValueString))
                {
                    failed[field.Item2] = $"Key was not present in the file";
                    continue;
                }

                try
                {
                    var value = Deserializer(fieldValueString, field.Item1.FieldType);
                    field.Item1.Set(field.Item4, value);
                }
                catch (Exception ex)
                {
                    failed[field.Item2] = $"Failed to deserialize: {ex.Message}";
                    continue;
                }
            }

            foreach (var prop in _configProps)
            {
                if (!collected.TryGetValue(prop.Item2, out var propValueString))
                {
                    failed[prop.Item2] = $"Key was not present in the file";
                    continue;
                }

                try
                {
                    var value = Deserializer(propValueString, prop.Item1.PropertyType);
                    prop.Item1.Set(prop.Item4, value);
                }
                catch (Exception ex)
                {
                    failed[prop.Item2] = $"Failed to deserialize: {ex.Message}";
                    continue;
                }
            }

            foreach (var dynamic in _configDynamic)
            {
                if (!collected.TryGetValue(dynamic.Item1, out var dynamicValueString))
                {
                    failed[dynamic.Item1] = $"Key was not present in the file";
                    continue;
                }

                try
                {
                    var value = Deserializer(dynamicValueString, dynamic.Item5);
                    dynamic.Item3(value);
                }
                catch (Exception ex)
                {
                    failed[dynamic.Item1] = $"Failed to deserialize: {ex.Message}";
                    continue;
                }
            }

            OnLoaded.Call(failed);

            DictionaryPool<string, string>.Shared.Return(collected);
            return failed;
        }

        public Dictionary<string, string> Save()
        {
            if (!IsValid)
                throw new InvalidOperationException($"This config file is missing a serializer/deserializer!");

            var writer = StringBuilderPool.Shared.Rent();
            var failed = new Dictionary<string, string>();

            foreach (var field in _configFields)
            {
                try
                {
                    Log?.Debug($"Serializing field: {field.Item1.ToName()}");

                    var serialized = Serializer(field.Item1.Get(field.Item4));

                    Log?.Debug(serialized);

                    if (!string.IsNullOrWhiteSpace(field.Item3))
                    {
                        var lines = field.Item3.SplitLines();

                        foreach (var comment in lines)
                            writer.AppendLine($"# {comment}");
                    }

                    writer.AppendLine($"[{field.Item2}]");
                    writer.AppendLine(serialized.Trim());
                    writer.AppendLine();
                }
                catch (Exception ex)
                {
                    failed[field.Item2] = $"Failed to serialize: {ex.Message}";
                    continue;
                }
            }

            foreach (var prop in _configProps)
            {
                try
                {
                    Log?.Debug($"Serializing property: {prop.Item1.ToName()}");

                    var serialized = Serializer(prop.Item1.Get(prop.Item4));

                    Log?.Debug(serialized);

                    if (!string.IsNullOrWhiteSpace(prop.Item3))
                    {
                        var lines = prop.Item3.SplitLines();

                        foreach (var comment in lines)
                            writer.AppendLine($"# {comment}");
                    }

                    writer.AppendLine($"[{prop.Item2}]");
                    writer.AppendLine(serialized.Trim());
                    writer.AppendLine();
                }
                catch (Exception ex)
                {
                    failed[prop.Item2] = $"Failed to serialize: {ex.Message}";
                    continue;
                }
            }

            foreach (var dyn in _configDynamic)
            {
                try
                {
                    Log?.Debug($"Serializing dynamic: {dyn.Item1}");

                    var serialized = Serializer(dyn.Item4());

                    Log?.Debug(serialized);

                    if (!string.IsNullOrWhiteSpace(dyn.Item2))
                    {
                        var lines = dyn.Item2.SplitLines();

                        foreach (var comment in lines)
                            writer.AppendLine($"# {comment}");
                    }

                    writer.AppendLine($"[{dyn.Item1}]");
                    writer.AppendLine(serialized.Trim());
                    writer.AppendLine();
                }
                catch (Exception ex)
                {
                    failed[dyn.Item1] = $"Failed to serialize: {ex.Message}";
                    continue;
                }
            }

            System.IO.File.WriteAllText(Path, StringBuilderPool.Shared.ToStringReturn(writer));
            return failed;
        }

        private void OnChanged()
            => Load();
    }
}