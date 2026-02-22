using System;
using System.Collections;
using System.Collections.Generic;

namespace FakerLibrary.Generators
{
    public class CollectionGenerator : IValueGenerator
    {
        public bool CanGenerate(Type type) =>
            type.IsArray
            || (type.IsGenericType &&
                (
                    type.GetGenericTypeDefinition() == typeof(List<>)
                    || type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    || type.GetGenericTypeDefinition() == typeof(ICollection<>)
                    || type.GetGenericTypeDefinition() == typeof(IList<>)
                ));

        public object Generate(Type type, GeneratorContext ctx)
        {
            Type elementType = type.IsArray
                ? type.GetElementType()
                : type.GetGenericArguments()[0];

            int size = ctx.Random.Next(3, 7);

            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < size; i++)
            {
                list.Add(ctx.Faker.Create(elementType));
            }

            if (type.IsArray)
            {
                var array = Array.CreateInstance(elementType, size);
                list.CopyTo(array, 0);
                return array;
            }
            return list;
        }
    }
}