using CommonLib.Extensions;

using System;
using System.Collections.Generic;

namespace CommonLib.Serialization
{
    public static class TypeCache
    {
        private static readonly Dictionary<string, Type> _previouslyRetrieved = new Dictionary<string, Type>();

        public static bool TryRetrieve(string typeName, out Type type)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                type = null;
                return false;
            }

            if (_previouslyRetrieved.TryGetValue(typeName, out type))
                return true;

            type = Type.GetType(typeName);

            if (type != null)
            {
                _previouslyRetrieved[typeName] = type;
                return true;
            }

            var types = CommonLibrary.SafeQueryTypes();

            if (types.TryGetFirst(t => t.FullName == typeName || t.Name == typeName, out type))
            {
                _previouslyRetrieved[typeName] = type;
                return true;
            }

            type = null;
            return false;
        }
    }
}