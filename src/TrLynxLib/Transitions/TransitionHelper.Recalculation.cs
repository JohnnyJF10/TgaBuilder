namespace TrLynxLib.Transitions;

public partial class TransitionHelper
{
    private const int RECALC_DELAY_MS = 50;

    private readonly object _recalcLock = new();
    private bool _recalcUpdateRunning;
    private bool _recalcUpdatePending;
    public async Task QueueRecalc(Action? Configure = null)
    {
        lock (_recalcLock)
        {
            if (_recalcUpdateRunning)
            {
                _recalcUpdatePending = true;
                return;
            }

            _recalcUpdateRunning = true;
        }

        try
        {
            do
            {
                lock (_recalcLock)
                {
                    _recalcUpdatePending = false;
                }

                Configure?.Invoke();

                await Task.Delay(RECALC_DELAY_MS);

                await Task.Run(Mix);

                RecalculationCompleted?.Invoke(this, EventArgs.Empty);
            }
            while (_recalcUpdatePending);
        }
        finally
        {
            lock (_recalcLock)
            {
                _recalcUpdateRunning = false;
            }
        }
    }
}