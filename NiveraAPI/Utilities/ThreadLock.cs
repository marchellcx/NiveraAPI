namespace NiveraAPI.Utilities;

public class ThreadLock
{
    private volatile bool _isLocked;

    /// <summary>
    /// 
    /// </summary>
    public bool IsLocked => _isLocked;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="onUnlocked"></param>
    /// <param name="mainThread"></param>
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
                // ignored
            }
            
            _isLocked = false;
        }
        else if (!mainThread)
        {
            ThreadPool.QueueUserWorkItem(_ =>
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
                    // ignored
                }

                _isLocked = false;
            });
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
                // ignored
            }
            
            _isLocked = false;
        }
    }
}