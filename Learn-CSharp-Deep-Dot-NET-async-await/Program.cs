// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;

for (int i = 0; i < 1000; i++)
{
    int capturedInt = i;
    MyThreadPool.QueueUserWorkItem(_ =>
    {
        Console.WriteLine(capturedInt);
        Thread.Sleep(1000);
    });
}

Console.WriteLine("Hello, World!");

static class MyThreadPool
{
    private static readonly BlockingCollection<(Action<object?>, ExecutionContext?)> WorkItems = new();

    public static void QueueUserWorkItem((Action<object?>, ExecutionContext?) action)
    {
        WorkItems.Add(action);
    }

    static MyThreadPool()
    {
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            new Thread(() =>
                {
                    while (true)
                    {
                        var (workItem, context) = WorkItems.Take();
                        if (context == null)
                        {
                            workItem(null);
                        }
                        else
                        {
                            ExecutionContext.Run(context, static state => ((Action)state!).Invoke(), workItem);
                        }
                    }
                }) { IsBackground = true }
                .Start();
        }
    }
}