using System.Text.RegularExpressions;

namespace CommonLib.Utilities
{
    public static class RegexUtils
    {
        public static readonly Regex CamelCaseRegex = new Regex("([A-Z])([A-Z]+)($|[A-Z])", RegexOptions.Compiled);

        public const string PascalCaseRegexString = "([a-z,0-9](?=[A-Z])|[A-Z](?=[A-Z][a-z]))";
        public const string NewLinesRegexString = "r\n|\r|\n";
    }
}