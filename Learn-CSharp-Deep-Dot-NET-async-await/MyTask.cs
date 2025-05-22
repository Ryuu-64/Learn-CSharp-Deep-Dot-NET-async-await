using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Learn_CSharp_Deep_Dot_NET_async_await;

public class MyTask
{
    private bool _isCompleted;
    private ExecutionContext? _context;
    private Action? _continuation;
    private Exception? _exception;

    public readonly struct Awaiter(MyTask task) : INotifyCompletion
    {
        public Awaiter GetAwaiter() => this;

        public bool IsCompleted => task.IsCompleted;

        public void OnCompleted(Action continuation)
        {
            task.ContinueWith(continuation);
        }

        public void GetResult() => task.Wait();
    }

    public Awaiter GetAwaiter() => new(this);

    private bool IsCompleted
    {
        get
        {
            lock (this)
            {
                return _isCompleted;
            }
        }
    }

    private void SetResult()
    {
        Complete(null);
    }

    private void SetException(Exception exception)
    {
        Complete(exception);
    }

    public void Wait()
    {
        ManualResetEventSlim? mres = null;
        lock (this)
        {
            if (!_isCompleted)
            {
                mres = new ManualResetEventSlim();
                ContinueWith(mres.Set);
            }
        }

        mres?.Wait();

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Throw(_exception);
        }
    }

    public static MyTask Run(Action action)
    {
        MyTask task = new();
        MyThreadPool.QueueUserWorkItem(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                task.SetException(e);
                return;
            }

            task.SetResult();
        });
        return task;
    }

    public static MyTask Run(Func<MyTask> action)
    {
        MyTask task = new();

        MyThreadPool.QueueUserWorkItem(RealAction);
        return task;

        void RealAction()
        {
            try
            {
                var next = action();
                next.ContinueWith(() =>
                {
                    if (next._exception is not null)
                    {
                        task.SetException(next._exception);
                    }
                    else
                    {
                        task.SetResult();
                    }
                });
            }
            catch (Exception e)
            {
                task.SetException(e);
            }
        }
    }

    public static MyTask Delay(int milliseconds)
    {
        MyTask task = new();
        new Timer(_ => task.SetResult()).Change(milliseconds, Timeout.Infinite);
        return task;
    }

    public static MyTask WhenAll(List<MyTask> tasks)
    {
        MyTask task = new();
        if (tasks.Count == 0)
        {
            task.SetResult();
        }
        else
        {
            int taskCount = tasks.Count;
            Action continuation = () =>
            {
                if (Interlocked.Decrement(ref taskCount) == 0)
                {
                    // TODO exception
                    task.SetResult();
                }
            };

            foreach (var t in tasks)
            {
                t.ContinueWith(continuation);
            }
        }

        return task;
    }

    public MyTask ContinueWith(Action continuation)
    {
        MyTask task = new();
        var realContinuation = () =>
        {
            try
            {
                continuation();
            }
            catch (Exception e)
            {
                task.SetException(e);
                return;
            }

            task.SetResult();
        };
        lock (this)
        {
            if (_isCompleted)
            {
                MyThreadPool.QueueUserWorkItem(realContinuation);
            }
            else
            {
                _continuation = realContinuation;
                _context = ExecutionContext.Capture();
            }
        }

        return task;
    }

    public MyTask ContinueWith(Func<MyTask> continuation)
    {
        MyTask task = new();
        var realContinuation = () =>
        {
            try
            {
                var next = continuation();
                next.ContinueWith(() =>
                {
                    if (next._exception is not null)
                    {
                        task.SetException(next._exception);
                    }
                    else
                    {
                        task.SetResult();
                    }
                });
            }
            catch (Exception e)
            {
                task.SetException(e);
            }
        };
        lock (this)
        {
            if (_isCompleted)
            {
                MyThreadPool.QueueUserWorkItem(realContinuation);
            }
            else
            {
                _continuation = realContinuation;
                _context = ExecutionContext.Capture();
            }
        }

        return task;
    }

    private void Complete(Exception? exception)
    {
        lock (this)
        {
            if (_isCompleted)
            {
                throw new InvalidOperationException("The task has already been completed.");
            }

            _isCompleted = true;
            _exception = exception;

            if (_continuation is not null)
            {
                MyThreadPool.QueueUserWorkItem(() =>
                {
                    if (_context is null)
                    {
                        _continuation();
                    }
                    else
                    {
                        ExecutionContext.Run(_context, static state => ((Action)state!).Invoke(), _continuation);
                    }
                });
            }
        }
    }

    public static MyTask Iterate(IEnumerable<MyTask> tasks)
    {
        var task = new MyTask();
        var enumerator = tasks.GetEnumerator();

        void MoveNext()
        {
            try
            {
                if (enumerator.MoveNext())
                {
                    var next = enumerator.Current;
                    next.ContinueWith(MoveNext);
                    return;
                }
            }
            catch (Exception e)
            {
                task.SetException(e);
                return;
            }

            task.SetResult();
        }

        MoveNext();

        return task;
    }
}