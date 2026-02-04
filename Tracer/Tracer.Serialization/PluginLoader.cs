using System.Reflection;
using Tracer.Serialization.Abstractions;

namespace Tracer.Serialization;

public class SerializationPluginLoader
{
    public List<ITraceResultSerializer> LoadPlugins(string path)
    {
        var plugins = new List<ITraceResultSerializer>();
        Console.WriteLine($"[Loader] Searching for plugins in: {path}");

        if (!Directory.Exists(path))
        {
            Console.WriteLine("[Loader] Directory does not exist!");
            return plugins;
        }

        var dlls = Directory.GetFiles(path, "*.dll");
        Console.WriteLine($"[Loader] Found {dlls.Length} .dll files in directory.");

        foreach (var dll in dlls)
        {
            try 
            {
                var assembly = Assembly.LoadFrom(dll);
                Console.WriteLine($"[Loader] Inspecting assembly: {assembly.GetName().Name}");

                // Получаем все типы и проверяем их
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // Проверяем, реализует ли тип интерфейс
                    if (typeof(ITraceResultSerializer).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        Console.WriteLine($"[Loader] Found valid plugin type: {type.FullName}");
                        var instance = Activator.CreateInstance(type) as ITraceResultSerializer;
                        if (instance != null) plugins.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Loader] Error loading {dll}: {ex.Message}");
            }
        }
        return plugins;
    }
}