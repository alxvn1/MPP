using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FakerLibrary.Generators;

namespace FakerLibrary
{
    public class Faker
    {
        private readonly List<IValueGenerator> _generators;
        private readonly Random _random = new();
        private readonly FakerConfig _config;
        private readonly HashSet<Type> _typeStack = new();

        public Faker(FakerConfig config = null)
        {
            _config = config;
            _generators = new List<IValueGenerator>
            {
                new IntGenerator(), new LongGenerator(), new DoubleGenerator(),
                new StringGenerator(), new DateTimeGenerator(), new CollectionGenerator()
            };
        }

        public T Create<T>() => (T)Create(typeof(T));

        public object Create(Type type, string memberName = null, Type ownerType = null)
        {
            
            if (ownerType != null && memberName != null)
            {
                var customGen = _config?.GetGenerator(ownerType, memberName);
                if (customGen != null) 
                    return customGen.Generate(type, new GeneratorContext(this, _random, ownerType, memberName));
            }

          
            var generator = _generators.FirstOrDefault(g => g.CanGenerate(type));
            if (generator != null) 
                return generator.Generate(type, new GeneratorContext(this, _random, ownerType, memberName));

           
            if (_typeStack.Contains(type)) return null; 
            _typeStack.Add(type);

            try
            {
                return CreateComplexObject(type);
            }
            finally
            {
                _typeStack.Remove(type); 
            }
        }

        private object CreateComplexObject(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)                       
                .OrderByDescending(c => c.GetParameters().Length);

            object instance = null;
            HashSet<string> initializedMembers = new(StringComparer.OrdinalIgnoreCase);

            foreach (var ctor in constructors)
            {
                try
                {
                    var parameters = ctor.GetParameters();
                    var args = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        args[i] = Create(parameters[i].ParameterType, parameters[i].Name, type);
                        initializedMembers.Add(parameters[i].Name);
                    }

                    instance = ctor.Invoke(args);
                    break;
                }
                catch { continue; }
            }

            if (instance == null && type.IsValueType) instance = Activator.CreateInstance(type);
            if (instance == null) return null;

            FillPropertiesAndFields(instance, type, initializedMembers);
            return instance;
        }

        private void FillPropertiesAndFields(object instance, Type type, HashSet<string> skip)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (var prop in type.GetProperties(flags).Where(p => p.CanWrite && p.SetMethod.IsPublic))
            {
                if (skip.Contains(prop.Name)) continue;
                prop.SetValue(instance, Create(prop.PropertyType, prop.Name, type));
            }

            foreach (var field in type.GetFields(flags))
            {
                if (skip.Contains(field.Name)) continue;
                field.SetValue(instance, Create(field.FieldType, field.Name, type));
            }
        }
    }
}