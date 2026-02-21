using NUnit.Framework;
using System.Threading;
using System.Linq;

// Создаем псевдоним 'CoreNS' для пространства имен, чтобы не было конфликтов
using CoreNS = Tracer.Core; 

namespace Tracer.Core.Tests;

[TestFixture]
public class TracerTests
{
    // Используем псевдоним для интерфейса
    private CoreNS.ITracer _tracer;

    [SetUp]
    public void Setup()
    {
        // Теперь компилятор точно знает, что мы берем класс Tracer из CoreNS
        _tracer = new CoreNS.Tracer(); 
    }

    [Test]
    public void SingleThread_SingleMethod_ShouldRecordCorrectName()
    {
        _tracer.StartTrace();
        Thread.Sleep(50);
        _tracer.StopTrace();

        var result = _tracer.GetTraceResult();
        var method = result.Threads[0].Methods[0];

        Assert.That(method.Name, Is.EqualTo("SingleThread_SingleMethod_ShouldRecordCorrectName"));
        Assert.That(method.ClassName, Is.EqualTo("TracerTests"));
        Assert.That(method.Time, Is.GreaterThanOrEqualTo(50));
    }
    

    [Test]
    public void NestedMethods_ShouldHaveCorrectHierarchy()
    {
        _tracer.StartTrace(); // Parent
        _tracer.StartTrace(); // Child
        _tracer.StopTrace();  // End Child
        _tracer.StopTrace();  // End Parent

        var result = _tracer.GetTraceResult();
        var parentMethod = result.Threads[0].Methods[0];

        Assert.That(parentMethod.Methods.Count, Is.EqualTo(1));
        Assert.That(result.Threads[0].Methods.Count, Is.EqualTo(1));
    }

    [Test]
    public void MultiThread_ShouldRecordMultipleThreads()
    {
        Thread t1 = new Thread(() => {
            _tracer.StartTrace();
            Thread.Sleep(20);
            _tracer.StopTrace();
        });

        _tracer.StartTrace();
        Thread.Sleep(10);
        _tracer.StopTrace();

        t1.Start();
        t1.Join();

        var result = _tracer.GetTraceResult();
        
        Assert.That(result.Threads.Count, Is.EqualTo(2));
    }
}