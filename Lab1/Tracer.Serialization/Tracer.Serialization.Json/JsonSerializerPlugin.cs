using System.Text.Json;
using System.IO; // <--- ОБЯЗАТЕЛЬНО ДОБАВЬ ЭТО
using Tracer.Core;
using Tracer.Serialization.Abstractions;

namespace Tracer.Serialization.Json;

public class JsonSerializerPlugin : ITraceResultSerializer
{
    public string Format => "json";

    public void Serialize(TraceResult traceResult, Stream to)
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };
        JsonSerializer.Serialize(to, traceResult, options);
    }
}