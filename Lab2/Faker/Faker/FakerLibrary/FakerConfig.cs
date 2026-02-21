using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FakerLibrary
{
    public class FakerConfig
    {
        private readonly Dictionary<(Type Owner, string Member), Type> _customGenerators = new();

        public void Add<TClass, TProperty, TGenerator>(Expression<Func<TClass, TProperty>> selector) 
            where TGenerator : IValueGenerator
        {
            if (selector.Body is MemberExpression memberExpression)
            {
                var memberName = memberExpression.Member.Name.ToLowerInvariant();
                _customGenerators[(typeof(TClass), memberName)] = typeof(TGenerator);
            }
            else throw new ArgumentException("Expression must be a member access (e.g., x => x.Name)");
        }

        public IValueGenerator GetGenerator(Type ownerType, string memberName)
        {
            if (ownerType != null && memberName != null)
            {
                if (_customGenerators.TryGetValue((ownerType, memberName.ToLowerInvariant()), out var genType))
                {
                    return (IValueGenerator)Activator.CreateInstance(genType); 
                }
            }
            return null;
        }
    }
}