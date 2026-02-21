using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Tracer.Core;
using Tracer.Serialization.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tracer.Serialization.Yaml;

public class YamlSerializerPlugin : ITraceResultSerializer
{
    public string Format => "yaml";

    // Те же DTO, что и для XML, чтобы YamlDotNet не ругался на отсутствие конструкторов
    private class MethodDto {
        public string Name { get; set; }
        public string Class { get; set; }
        public string Time { get; set; }
        public List<MethodDto> Methods { get; set; }
    }

    private class ThreadDto {
        public int Id { get; set; }
        public string Time { get; set; }
        public List<MethodDto> Methods { get; set; }
    }

    private class ResultDto {
        public List<ThreadDto> Threads { get; set; }
    }

    public void Serialize(TraceResult traceResult, Stream to)
    {
        var dto = new ResultDto {
            Threads = traceResult.Threads.Select(t => new ThreadDto {
                Id = t.Id,
                Time = $"{t.Time}ms",
                Methods = MapMethods(t.Methods)
            }).ToList()
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        using var writer = new StreamWriter(to, leaveOpen: true);
        serializer.Serialize(writer, dto);
        writer.Flush();
    }

    private List<MethodDto> MapMethods(IReadOnlyList<MethodResult> methods) {
        return methods.Select(m => new MethodDto {
            Name = m.Name,
            Class = m.ClassName,
            Time = $"{m.Time}ms",
            Methods = MapMethods(m.Methods)
        }).ToList();
    }
}