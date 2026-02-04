using System.IO; // <--- Здесь тоже должен быть
using Tracer.Core;

namespace Tracer.Serialization.Abstractions;

public interface ITraceResultSerializer
{
    string Format { get; }
    void Serialize(TraceResult traceResult, Stream to);
}