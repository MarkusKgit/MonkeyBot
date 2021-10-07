using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MonkeyBot.UnitTests.Utils
{
    internal static class PropertyMatcher
    {
        private static IDictionary<string, PropertyInfo[]> _memo = new Dictionary<string, PropertyInfo[]>();

        internal static void Match<T>(T expected, T actual)
        {
            var properties = GetProperties<T>();
            var validationErrors = new List<string>();
            foreach (var property in properties)
            {
                var expectedValue = property.GetValue(expected);
                var actualValue = property.GetValue(actual);

                if (expectedValue == null && actualValue == null)
                {
                    continue;
                }

                if (expectedValue == null || actualValue == null || !expectedValue.Equals(actualValue))
                {
                    validationErrors.Add($"Property {property.Name} Expected {expectedValue} but was {actualValue}");
                }
            }
            Assert.True(!validationErrors.Any(), string.Join(Environment.NewLine, validationErrors));
        }

        private static PropertyInfo[] GetProperties<T>()
        {
            var entityType = typeof(T);
            if(!_memo.TryGetValue(entityType.FullName, out var properties))
            {
                var entityProperties = entityType.GetProperties();
                _memo.Add(entityType.FullName, entityProperties);

                return entityProperties;
            }
            return properties;
        }
    }
}
