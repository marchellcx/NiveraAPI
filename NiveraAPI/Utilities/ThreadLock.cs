namespace NiveraAPI.Utilities;

public class ThreadLock
{
    private volatile bool _isLocked;

    public bool IsLocked => _isLocked;

    public void Request(Action onUnlocked, bool mainThread = true)
    {
        if (!_isLocked)
        {
            _isLocked = true;
            try
            {
                onUnlocked();
            }
            catch
            {
            }
            _isLocked = false;
        }
        else if (!mainThread)
        {
            new Thread((ThreadStart)delegate
            {
                while (_isLocked)
                {
                }
                _isLocked = true;
                try
                {
                    onUnlocked();
                }
                catch
                {
                }
                _isLocked = false;
            }).Start();
        }
        else
        {
            while (_isLocked)
            {
            }
            _isLocked = true;
            try
            {
                onUnlocked();
            }
            catch
            {
            }
            _isLocked = false;
        }
    }
}