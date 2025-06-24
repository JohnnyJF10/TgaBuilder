using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace THelperLib.Utils
{
    public class PerformanceTracker
    {
        private readonly List<long> _measurements = new();
        private readonly object _lock = new();
        private Stopwatch? _stopwatch;
        private int _counter = 0;

        public void StartMeasure()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        public void EndMeasure()
        {
            if (_stopwatch == null)
            {
                Debug.WriteLine("Warning: EndMeasure called without StartMeasure.");
                return;
            }

            _stopwatch.Stop();
            long ticks = _stopwatch.ElapsedTicks;
            double microseconds = (ticks * 1_000_000.0) / Stopwatch.Frequency;

            lock (_lock)
            {
                _measurements.Add((long)microseconds);
                _counter++;

                double average = _measurements.Average();

                Debug.WriteLine($"Measurement #{_counter}: {microseconds:F2} µs | Average: {average:F2} µs");     
            }

            _stopwatch = null;
        }

        public void Reset()
        {
            lock (_lock)
            {
                _measurements.Clear();
                _counter = 0;
            }
        }
    }
}
