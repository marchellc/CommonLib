using System;

namespace CommonLib.Logging
{
    public struct LogCharacter
    {
        public readonly char Character;
        public readonly ConsoleColor Color;

        public LogCharacter(char c, ConsoleColor color = ConsoleColor.White)
        {
            Character = c;
            Color = color;
        }
    }
}