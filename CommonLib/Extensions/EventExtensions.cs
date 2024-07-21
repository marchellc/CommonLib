using CommonLib.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CommonLib.Extensions
{
    public static class EventExtensions
    {
        private static readonly Dictionary<Type, EventInfo[]> _events = new Dictionary<Type, EventInfo[]>();
        private static readonly BindingFlags _flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static LogOutput Log = new LogOutput("Event Extensions").Setup();

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
                throw new ArgumentException($"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            if (!method.TryCreateDelegate(target, ev.EventHandlerType, out var del))
                return;

            try
            {
                ev.RemoveEventHandler(target, del);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove event handler '{method.ToName()}' from event '{ev.ToName()}':\n{ex}");
            }

            try
            {
                ev.AddEventHandler(target, del);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to add event handler '{method.ToName()}' to event '{ev.ToName()}':\n{ex}");
            }
        }

        public static void AddHandler(this Type type, string eventName, MethodInfo method)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException($"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            if (!method.TryCreateDelegate(ev.EventHandlerType, out var del))
                return;

            try
            {
                ev.RemoveEventHandler(null, del);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove event handler '{method.ToName()}' from event '{ev.ToName()}':\n{ex}");
            }

            try
            {
                ev.AddEventHandler(null, del);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to add event handler '{method.ToName()}' to event '{ev.ToName()}':\n{ex}");
            }
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
                throw new ArgumentException($"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            try
            {
                ev.RemoveEventHandler(handler.Target, handler);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove event handler '{handler.GetMethodInfo().ToName()}' from event '{ev.ToName()}':\n{ex}");
            }

            try
            {
                ev.AddEventHandler(handler.Target, handler);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to add event handler '{handler.GetMethodInfo().ToName()}' to event '{ev.ToName()}':\n{ex}");
            }
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
                throw new ArgumentException($"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            try
            {
                ev.RemoveEventHandler(handler.Target, handler);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove event handler '{handler.GetMethodInfo().ToName()}' from event '{ev.ToName()}':\n{ex}");
            }
        }

        public static void Raise(this EventInfo ev, object instance, params object[] args)
        {
            if (ev is null)
                throw new ArgumentNullException(nameof(ev));

            var evDelegateField = ev.DeclaringType.Field(ev.Name);

            if (evDelegateField is null)
            {
                Log.Warn($"Tried to raise an event with a missing delegate field: {ev.ToName()}");
                return;
            }

            var evDelegate = evDelegateField.Get<MulticastDelegate>(instance);

            if (evDelegate is null)
                return;

            try
            {
                evDelegate.DynamicInvoke(args);
            }
            catch (Exception ex)
            {
                Log.Error($"An exception occured while invoking event '{ev.ToName()}':\n{ex}");
            }
        }
    }
}