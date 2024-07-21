using System.Collections.Generic;

namespace CommonLib.Extensions
{
    public static class StackExtensions
    {
        public static bool TryPop<T>(this Stack<T> stack, out T value)
        {
            if (stack.Count <= 0)
            {
                value = default;
                return false;
            }

            try
            {
                value = stack.Pop();
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public static bool TryPush<T>(this Stack<T> stack, T value)
        {
            try
            {
                stack.Push(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}