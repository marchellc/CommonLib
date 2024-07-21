using System;

namespace CommonLib.Configs
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class ConfigAttribute : Attribute
    {
        public string Name { get; }
        public string[] Description { get; }

        public ConfigAttribute(string name, params string[] descriptionLines)
        {
            Name = name;
            Description = descriptionLines;
        }
    }
}