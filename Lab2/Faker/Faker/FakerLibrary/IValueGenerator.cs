using System;

namespace FakerLibrary
{
    public interface IValueGenerator
    {
        object Generate(Type type, GeneratorContext context);
        bool CanGenerate(Type type);
    }
}