// Tracer.cs
using System.Diagnostics;
using System.Collections.Concurrent;


namespace Tracer.Core;

public class Tracer : ITracer
{
    private readonly ConcurrentDictionary<int, ThreadContext> _threads = new();

    public void StartTrace()
    {
        var threadId = Environment.CurrentManagedThreadId;
        var context = _threads.GetOrAdd(threadId, _ => new ThreadContext(threadId));
        
        var stackTrace = new StackTrace();
        var method = stackTrace.GetFrame(1)?.GetMethod();
        
        context.StartMethod(method?.Name ?? "Unknown", method?.DeclaringType?.Name ?? "Unknown");
    }

    public void StopTrace()
    {
        if (_threads.TryGetValue(Environment.CurrentManagedThreadId, out var context))
        {
            context.StopMethod();
        }
    }

    public TraceResult GetTraceResult()
    {
        return new TraceResult(_threads.Values.Select(t => t.ToResult()).ToList());
    }
}

// Вспомогательный класс для дерева методов внутри потока
internal class ThreadContext
{
    public int Id { get; }
    private readonly List<MethodTracker> _rootMethods = new();
    private readonly Stack<MethodTracker> _stack = new();

    public ThreadContext(int id) => Id = id;

    public void StartMethod(string name, string className)
    {
        var tracker = new MethodTracker(name, className);
        if (_stack.Count > 0)
            _stack.Peek().AddChild(tracker);
        else
            _rootMethods.Add(tracker);
        
        _stack.Push(tracker);
        tracker.Stopwatch.Start();
    }

    public void StopMethod()
    {
        if (_stack.Count > 0)
        {
            var tracker = _stack.Pop();
            tracker.Stopwatch.Stop();
        }
    }

    public ThreadResult ToResult()
    {
        var methods = _rootMethods.Select(m => m.ToResult()).ToList();
        return new ThreadResult(Id, methods.Sum(m => m.Time), methods);
    }
}

internal class MethodTracker
{
    public string Name { get; }
    public string ClassName { get; }
    public Stopwatch Stopwatch { get; } = new();
    private readonly List<MethodTracker> _children = new();

    public MethodTracker(string name, string className)
    {
        Name = name;
        ClassName = className;
    }

    public void AddChild(MethodTracker child) => _children.Add(child);

    public MethodResult ToResult() => 
        new MethodResult(Name, ClassName, Stopwatch.ElapsedMilliseconds, _children.Select(c => c.ToResult()).ToList());
}