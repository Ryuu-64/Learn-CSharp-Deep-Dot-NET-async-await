using System.Collections.Concurrent;

namespace Learn_CSharp_Deep_Dot_NET_async_await;

public static class MyThreadPool
{
    private static readonly BlockingCollection<(Action, ExecutionContext?)> WorkItems = new();

    static MyThreadPool()
    {
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            new Thread(Start) { IsBackground = true }.Start();
        }
    }

    public static void QueueUserWorkItem(Action action)
    {
        WorkItems.Add((action, ExecutionContext.Capture()));
    }

    private static void Start()
    {
        while (true)
        {
            var (workItem, context) = WorkItems.Take();
            if (context is null)
            {
                workItem();
            }
            else
            {
                ExecutionContext.Run(context, static state => ((Action)state!).Invoke(), workItem);
            }
        }
    }
}