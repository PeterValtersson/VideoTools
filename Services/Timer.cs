using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoTools.Services
{
    public class Timer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        public event Action OnTick;
        private readonly int _delay;
        public bool IsRunning { get; private set; }
        public Timer(int delay = 1000)
        {
            _delay = delay;
        }
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task Start()
        {
            IsRunning = true;
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Run(() => { OnTick?.Invoke(); });
                    await Task.Delay(_delay, _cancellationTokenSource.Token);
                }
            }
            catch (TaskCanceledException) { }
            catch { }
            IsRunning = false;
        }
    }
}
