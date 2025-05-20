// See https://aka.ms/new-console-template for more information

namespace Learn_CSharp_Deep_Dot_NET_async_await;

public static class Program
{
    public static void Main()
    {
        // List<MyTask> tasks = [];
        // AsyncLocal<int> asyncLocalValue = new();
        // for (var i = 0; i < 100; i++)
        // {
        //     asyncLocalValue.Value = i;
        //     tasks.Add(MyTask.Run(() =>
        //     {
        //         Console.WriteLine(asyncLocalValue.Value);
        //         Thread.Sleep(1000);
        //     }));
        // }
        // MyTask.WhenAll(tasks).Wait();

        Console.Write("Hello");
        MyTask.Delay(1000)
            .ContinueWith(() =>
            {
                Console.WriteLine(" World.");
                return MyTask.Delay(2000);
            })
            .ContinueWith(() => { Console.WriteLine("How are you?"); })
            .Wait();
    }
}