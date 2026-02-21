using System;

namespace FakerLibrary.Generators
{
    public class IntGenerator : IValueGenerator {
        public bool CanGenerate(Type type) => type == typeof(int);
        public object Generate(Type type, GeneratorContext ctx) => ctx.Random.Next();
    }

    public class LongGenerator : IValueGenerator {
        public bool CanGenerate(Type type) => type == typeof(long);
        public object Generate(Type type, GeneratorContext ctx) => (long)(ctx.Random.NextDouble() * long.MaxValue);
        
    }

    public class DoubleGenerator : IValueGenerator {
        public bool CanGenerate(Type type) => type == typeof(double);
        public object Generate(Type type, GeneratorContext ctx) => ctx.Random.NextDouble() * 100.0;
    }

    public class StringGenerator : IValueGenerator {
        public bool CanGenerate(Type type) => type == typeof(string);
        public object Generate(Type type, GeneratorContext ctx) => Guid.NewGuid().ToString().Substring(0, 10);
    }

    public class DateTimeGenerator : IValueGenerator {
        public bool CanGenerate(Type type) => type == typeof(DateTime);
        public object Generate(Type type, GeneratorContext ctx) => 
            DateTime.Now.AddDays(ctx.Random.Next(-10000, 10000));
    }
}