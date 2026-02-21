using System.Threading;
using Tracer.Core;

namespace Tracer.Example;

public class Foo
{
    private readonly ITracer _tracer;

    public Foo(ITracer tracer)
    {
        _tracer = tracer;
    }

    public void MyMethod()
    {
        _tracer.StartTrace();
        Thread.Sleep(100); // Имитация работы
        
        InnerMethod(); // Вложенный вызов
        
        _tracer.StopTrace();
    }

    private void InnerMethod()
    {
        _tracer.StartTrace();
        Thread.Sleep(50);
        _tracer.StopTrace();
    }
}