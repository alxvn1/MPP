using System;
using System.Collections.Generic;
using NUnit.Framework;
using FakerLibrary;
using FakerLibrary.Generators;

namespace FakerTests
{
    public class User
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime RegDate { get; set; }
        public List<double> Prices { get; set; }
    }

    public class Node
    {
        public Node Next { get; set; }
    }

    public class ImmutablePerson
    {
        public string City { get; }
        public ImmutablePerson(string city) => City = city;
    }

    public class CityGenerator : IValueGenerator
    {
        public bool CanGenerate(Type t) => true;
        public object Generate(Type t, GeneratorContext c) => "Minsk";
    }

    [TestFixture]
    public class FakerTests
    {
        [Test]
        public void Test_BasicTypesAndCollections()
        {
            var faker = new Faker();
            var user = faker.Create<User>();

            Assert.Multiple(() =>
            {
                Assert.That(user.Name, Is.Not.Null);
                Assert.That(user.RegDate, Is.Not.EqualTo(default(DateTime)));
                Assert.That(user.Prices, Is.Not.Null.And.Not.Empty);
            });
        }

        [Test]
        public void Test_Cycles()
        {
            var faker = new Faker();
            var node = faker.Create<Node>();

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Next, Is.Null);
        }

        [Test]
        public void Test_ExpressionConfig()
        {
            var config = new FakerConfig();
            config.Add<User, string, CityGenerator>(u => u.Name);
            var faker = new Faker(config);

            var user = faker.Create<User>();

            Assert.That(user.Name, Is.EqualTo("Minsk"));
        }

        [Test]
        public void Test_ImmutableWithConstructorConfig()
        {
            var config = new FakerConfig();
            config.Add<ImmutablePerson, string, CityGenerator>(p => p.City);
            var faker = new Faker(config);

            var person = faker.Create<ImmutablePerson>();
            Assert.That(person.City, Is.EqualTo("Minsk"));
        }
    }
}