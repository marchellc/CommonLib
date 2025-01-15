using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace CommonLib.Extensions
{
    public static class EventExtensions
    {
        private static readonly Dictionary<Type, EventInfo[]> _events = new Dictionary<Type, EventInfo[]>();
        private static readonly BindingFlags _flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static EventInfo Event(this Type type, string eventName, bool ignoreCase = false)
            => GetAllEvents(type).FirstOrDefault(ev => ignoreCase ? ev.Name.ToLower() == eventName.ToLower() : ev.Name == eventName);

        public static EventInfo[] GetAllEvents(this Type type)
        {
            if (_events.TryGetValue(type, out var events))
                return events;

            return _events[type] = type.GetEvents(_flags);
        }

        public static void AddHandler(this Type type, string eventName, MethodInfo method, object target)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException(
                    $"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            if (!method.TryCreateDelegate(target, ev.EventHandlerType, out var del))
                return;

            ev.AddEventHandler(target, del);
        }

        public static void AddHandler(this Type type, string eventName, MethodInfo method)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException(
                    $"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            if (!method.TryCreateDelegate(ev.EventHandlerType, out var del))
                return;

            ev.AddEventHandler(null, del);
        }

        public static void AddHandler(this Type type, string eventName, Delegate handler)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException(
                    $"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            ev.AddEventHandler(handler.Target, handler);
        }

        public static void RemoveHandler(this Type type, string eventName, Delegate handler)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException(
                    $"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            ev.RemoveEventHandler(handler.Target, handler);
        }

        public static void Raise(this EventInfo ev, object instance, params object[] args)
        {
            if (ev is null)
                throw new ArgumentNullException(nameof(ev));

            var evDelegateField = ev.DeclaringType.Field(ev.Name);

            if (evDelegateField is null)
                return;

            var evDelegate = evDelegateField.Get<MulticastDelegate>(instance);

            if (evDelegate is null)
                return;

            evDelegate.DynamicInvoke(args);
        }
    }
}