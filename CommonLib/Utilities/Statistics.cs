using System;

namespace CommonLib.Utilities
{
    public class Statistics<T>
    {
        private T _value;

        private bool _highSet;
        private bool _lowSet;

        private Func<T, T, T> _avgCalc;

        private Func<T, T, bool> _highComp;
        private Func<T, T, bool> _lowComp;

        public T Highest { get; private set; }
        public T Lowest { get; private set; }

        public T Average => _avgCalc(Highest, Lowest);

        public T Value
        {
            get => _value;
            set
            {
                _value = value;

                if (!_highSet || _highComp(Highest, value))
                {
                    Highest = value;
                    _highSet = true;
                }

                if (!_lowSet || !_lowComp(Lowest, value))
                {
                    Lowest = value;
                    _lowSet = true;
                }
            }
        }

        public Statistics(Func<T, T, T> averageCalculator, Func<T, T, bool> highestComparer, Func<T, T, bool> lowestComparer)
        {
            _avgCalc = averageCalculator;
            _highComp = highestComparer;
            _lowComp = lowestComparer;
        }
    }
}