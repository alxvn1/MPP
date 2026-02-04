using System;
using System.IO;
using System.Threading;
using Tracer.Core;
using Tracer.Serialization;

namespace Tracer.Example;

class Program
{
    static void Main(string[] args)
    {
        ITracer tracer = new Tracer.Core.Tracer();
        Foo foo = new Foo(tracer);

        // 1. Запуск трассировки
        foo.MyMethod();

        Thread thread = new Thread(() => {
            foo.MyMethod();
        });
        thread.Start();
        thread.Join();

        TraceResult result = tracer.GetTraceResult();

        // 2. Путь к плагинам
        string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        Console.WriteLine($"[Main] Absolute plugins path: {Path.GetFullPath(pluginsPath)}");

        if (!Directory.Exists(pluginsPath)) 
        {
            Directory.CreateDirectory(pluginsPath);
            Console.WriteLine("[Main] Plugins directory created.");
        }

        // 3. ЗАГРУЗКА (Этой строки не хватало)
        var loader = new SerializationPluginLoader();
        var serializers = loader.LoadPlugins(pluginsPath); // Теперь 'serializers' определен

        Console.WriteLine($"[Main] Found {serializers.Count} plugins.");

        // 4. СЕРИАЛИЗАЦИЯ
        foreach (var serializer in serializers)
        {
            string fileName = $"result.{serializer.Format}";
            
            // Используем явное указание System.IO.File, чтобы убрать Ambiguous invocation
            using (FileStream fs = System.IO.File.Create(fileName)) 
            {
                serializer.Serialize(result, fs);
            }
            Console.WriteLine($"[Main] Result saved to {fileName}");
        }

        Console.WriteLine("Done. Press any key...");
        Console.ReadKey();
    }
}