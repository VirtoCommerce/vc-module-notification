using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer.Filters
{
    public static class ArrayFilter
    {
        /// <summary>
        /// Filter the elements of an array by a given condition
        /// {% assign filtered = items | where: 'propertyName' '==' 'propertyValue' %}
        /// </summary>
        /// <param name="input"></param>
        /// <param name="propertyName"></param>
        /// <param name="operationName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static object Where(object input, string propertyName, string operationName, string propertyValue)
        {
            if (input is not IEnumerable enumerable)
            {
                throw new ArgumentException("Input is not a collection", nameof(input));
            }

            var elementType = GetEnumerableElementType(enumerable.GetType());
            var property = elementType.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property is null)
            {
                throw new ArgumentException($"Unknown property '{propertyName}'", nameof(propertyName));
            }

            var parameterX = Expression.Parameter(elementType, "x");
            var left = Expression.Property(parameterX, property);
            var value = ParsePropertyValue(propertyValue, left.Type);
            var right = Expression.Constant(value, left.Type);

            BinaryExpression operation;

            if (operationName.EqualsIgnoreCase("=="))
            {
                operation = Expression.Equal(left, right);
            }
            else if (operationName.EqualsIgnoreCase("!="))
            {
                operation = Expression.NotEqual(left, right);
            }
            else if (operationName.EqualsIgnoreCase(">"))
            {
                operation = Expression.GreaterThan(left, right);
            }
            else if (operationName.EqualsIgnoreCase(">="))
            {
                operation = Expression.GreaterThanOrEqual(left, right);
            }
            else if (operationName.EqualsIgnoreCase("<"))
            {
                operation = Expression.LessThan(left, right);
            }
            else if (operationName.EqualsIgnoreCase("<="))
            {
                operation = Expression.LessThanOrEqual(left, right);
            }
            else if (operationName.EqualsIgnoreCase("contains"))
            {
                Expression expression;

                if (property.PropertyType == typeof(string))
                {
                    var containsMethod = typeof(string).GetMethods().First(x => x.Name == "Contains");
                    expression = Expression.Call(left, containsMethod, right);
                }
                else
                {
                    var containsMethod = typeof(Enumerable).GetMethods()
                        .First(x => x.Name == "Contains" && x.GetParameters().Length == 2)
                        .MakeGenericMethod(value.GetType());

                    expression = Expression.Call(containsMethod, left, right);
                }

                operation = Expression.Equal(expression, Expression.Constant(true));
            }
            else
            {
                throw new ArgumentException($"Unknown operation '{operationName}'", nameof(operationName));
            }

            var delegateType = typeof(Func<,>).MakeGenericType(elementType, typeof(bool));

            // Construct expression: Func<T, bool> = (x) => x.propertyName == propertyValue
            var lambda = Expression.Lambda(delegateType, operation, parameterX);

            // Find Queryable.Where(Expression<Func<TSource, bool>>) method
            var whereMethod = typeof(Queryable).GetMethods()
                .Where(x => x.Name == "Where")
                .Select(x => new { M = x, P = x.GetParameters() })
                .Where(x => x.P.Length == 2 &&
                            x.P[0].ParameterType.IsGenericType &&
                            x.P[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                            x.P[1].ParameterType.IsGenericType &&
                            x.P[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>))
                .Select(x => new { x.M, A = x.P[1].ParameterType.GetGenericArguments() })
                .Where(x => x.A[0].IsGenericType &&
                            x.A[0].GetGenericTypeDefinition() == typeof(Func<,>))
                .Select(x => new { x.M, A = x.A[0].GetGenericArguments() })
                .Where(x => x.A[0].IsGenericParameter &&
                            x.A[1] == typeof(bool))
                .Select(x => x.M)
                .SingleOrDefault();

            var result = whereMethod?.MakeGenericMethod(elementType).Invoke(null, new object[] { enumerable.AsQueryable(), lambda });

            return result;
        }


        private static object ParsePropertyValue(string value, Type valueType)
        {
            if ((valueType == typeof(int) || valueType == typeof(int?)) && int.TryParse(value, out var intValue))
            {
                return intValue;
            }

            if (valueType == typeof(double) && double.TryParse(value, out var doubleValue))
            {
                return doubleValue;
            }

            if (valueType == typeof(decimal) && decimal.TryParse(value, out var decimalValue))
            {
                return decimalValue;
            }

            if (valueType == typeof(TimeSpan) && TimeSpan.TryParse(value, out var timespan))
            {
                return timespan;
            }

            if (valueType == typeof(DateTime) && DateTime.TryParse(value, out var dateTime))
            {
                return dateTime;
            }

            if (valueType == typeof(char) && char.TryParse(value, out var charValue))
            {
                return charValue;
            }

            if (valueType == typeof(bool) && bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }

            return value;
        }

        private static Type GetEnumerableElementType(Type type)
        {
            return type.GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(x => x.GetGenericArguments()[0])
                .FirstOrDefault();
        }
    }
}
