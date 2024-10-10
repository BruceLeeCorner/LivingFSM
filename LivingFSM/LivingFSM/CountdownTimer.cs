using System.Diagnostics;

namespace LivingFSM
{
    public class CountdownTimer
    {
        private readonly Stopwatch _stopWatch;
        private int _target;

        public CountdownTimer(int targetMS)
        {
            _target = targetMS;
            _stopWatch = Stopwatch.StartNew();
        }

        public bool IsTimeOut => _stopWatch.Elapsed.TotalMilliseconds >= _target;

        public void Restart(int targetMS)
        {
            _target = targetMS;
            _stopWatch.Restart();
        }
    }
}