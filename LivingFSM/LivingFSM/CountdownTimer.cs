using System.Diagnostics;

namespace LivingFSM
{
    public class CountdownTimer
    {
        private readonly Stopwatch _stopWatch;
        private int _value;

        public CountdownTimer(int value)
        {
            _value = value;
            _stopWatch = Stopwatch.StartNew();
        }

        public bool IsTimeOut => _stopWatch.Elapsed.TotalMilliseconds >= _value;

        public void Restart(int value)
        {
            _value = value;
            _stopWatch.Restart();
        }
    }
}