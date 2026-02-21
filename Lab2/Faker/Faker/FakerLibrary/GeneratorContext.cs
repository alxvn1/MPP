using System;

namespace FakerLibrary
{
    public class GeneratorContext
    {
        public Faker Faker { get; }
        public Random Random { get; }
        public Type OwnerType { get; }
        public string MemberName { get; }

        public GeneratorContext(Faker faker, Random random, Type ownerType = null, string memberName = null)
        {
            Faker = faker;
            Random = random;
            OwnerType = ownerType;
            MemberName = memberName;
        }
    }
}