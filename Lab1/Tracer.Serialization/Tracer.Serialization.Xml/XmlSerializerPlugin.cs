using System; // Добавь этот using
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Tracer.Core;
using Tracer.Serialization.Abstractions;

namespace Tracer.Serialization.Xml;

public class XmlSerializerPlugin : ITraceResultSerializer
{
    public string Format => "xml";

    // Наши DTO классы...
    public class MethodDto {
        [XmlAttribute("name")] public string Name { get; set; }
        [XmlAttribute("class")] public string ClassName { get; set; }
        [XmlAttribute("time")] public string Time { get; set; }
        [XmlElement("method")] public List<MethodDto> Methods { get; set; } = new();
    }

    public class ThreadDto {
        [XmlAttribute("id")] public int Id { get; set; }
        [XmlAttribute("time")] public string Time { get; set; }
        [XmlElement("method")] public List<MethodDto> Methods { get; set; } = new();
    }

    [XmlRoot("root")]
    public class TraceResultDto {
        [XmlElement("thread")] public List<ThreadDto> Threads { get; set; } = new();
    }

    public void Serialize(TraceResult traceResult, Stream to)
    {
        // МАРКЕР: Если ты не увидишь эту надпись в консоли - значит DLL старая!
        Console.WriteLine("[XmlPlugin] Successfully started serialization using NEW DTO version.");

        var dto = new TraceResultDto {
            Threads = traceResult.Threads.Select(t => new ThreadDto {
                Id = t.Id,
                Time = $"{t.Time}ms",
                Methods = MapMethods(t.Methods)
            }).ToList()
        };

        var serializer = new XmlSerializer(typeof(TraceResultDto));
        serializer.Serialize(to, dto);
    }

    private List<MethodDto> MapMethods(IReadOnlyList<MethodResult> methods) {
        return methods.Select(m => new MethodDto {
            Name = m.Name,
            ClassName = m.ClassName,
            Time = $"{m.Time}ms",
            Methods = MapMethods(m.Methods)
        }).ToList();
    }
}