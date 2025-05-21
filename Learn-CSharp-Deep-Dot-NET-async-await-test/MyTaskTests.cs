using System.Diagnostics;
using Learn_CSharp_Deep_Dot_NET_async_await;

namespace Learn_CSharp_Deep_Dot_NET_async_await_test;

public class MyTaskTests
{
    [Test]
    public void ShouldExecuteSequentially()
    {
        var result = "";
        var stepCounter = 0;

        MyTask.Run(() =>
            {
                result += "Hello";
                stepCounter++;
                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.EqualTo("Hello"));
                    Assert.That(stepCounter, Is.EqualTo(1));
                });
                return MyTask.Delay(2000);
            })
            .ContinueWith(() =>
            {
                result += " World";
                stepCounter++;
                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.EqualTo("Hello World"));
                    Assert.That(stepCounter, Is.EqualTo(2));
                });
                return MyTask.Delay(2000);
            })
            .ContinueWith(() =>
            {
                result += ".";
                stepCounter++;
                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.EqualTo("Hello World."));
                    Assert.That(stepCounter, Is.EqualTo(3));
                });
            })
            .Wait();
    }

    [Test]
    public void ShouldExecuteIterate()
    {
        var result = "";
        var startTime = DateTime.Now;
        MyTask.Iterate(PrintAsync(10)).Wait();
        var totalTime = DateTime.Now - startTime;
        Assert.Multiple(() =>
        {
            Assert.That(totalTime.TotalMilliseconds, Is.InRange(1_000 - 100, 1_000 + 100));
            Assert.That(result, Is.EqualTo("0123456789"));
        });
        return;

        IEnumerable<MyTask> PrintAsync(int count)
        {
            for (var i = 0; i < count; i++)
            {
                result += i;
                yield return MyTask.Delay(100);
            }
        }
    }

    [Test]
    public void ShouldWaitForAllTask()
    {
        var result = "";
        List<MyTask> tasks = [];
        AsyncLocal<int> asyncLocalValue = new();
        for (var i = 0; i < 10; i++)
        {
            asyncLocalValue.Value = i;
            tasks.Add(MyTask.Run(() =>
            {
                Thread.Sleep(asyncLocalValue.Value * 100);
                result += asyncLocalValue.Value;
            }));
        }

        Assert.That(result, Is.EqualTo(""));
        MyTask.WhenAll(tasks).Wait();
        Assert.That(result, Is.EqualTo("0123456789"));
    }


    [Test]
    public async Task ShouldAwait()
    {
        var stopwatch = Stopwatch.StartNew();

        var result = "";
        result += "Hello";
        await MyTask.Delay(1000);
        result += " World";

        stopwatch.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("Hello World"));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.InRange(900, 1100));
        });
    }
}