using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Extensions;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer.Filters
{
    public static partial class ArrayFilter
    {
        /// <summary>
        /// Filter the elements of an array by a given condition
        /// {% assign sorted = pages | where:"propName","==","value" %}
        /// </summary>
        /// <param name="input"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public static object Where(object input, string propName, string op, string value)
        {
            var retVal = input;
            var enumerable = retVal as IEnumerable;
            if (enumerable != null)
            {
                var queryable = enumerable.AsQueryable();
                var elementType = GetEnumerableType(enumerable.GetType());

                var paramX = Expression.Parameter(elementType, "x");
                var propInfo = elementType.GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                var left = Expression.Property(paramX, propInfo);
                var objValue = ParseString(value, left.Type);


                ConstantExpression right = Expression.Constant(objValue,left.Type);
                BinaryExpression binaryOp;

                if (op.EqualsInvariant("=="))
                    binaryOp = Expression.Equal(left, right);
                else if (op.EqualsInvariant("!="))
                    binaryOp = Expression.NotEqual(left, right);
                else if (op.EqualsInvariant(">"))
                    binaryOp = Expression.GreaterThan(left, right);
                else if (op.EqualsInvariant(">="))
                    binaryOp = Expression.GreaterThanOrEqual(left, right);
                else if (op.EqualsInvariant("=<"))
                    binaryOp = Expression.LessThan(left, right);
                else if (op.EqualsInvariant("contains"))
                {
                    Expression expr = null;
                    if (propInfo.PropertyType == typeof(string))
                    {
                        var containsMethod = typeof(string).GetMethods().First(x => x.Name == "Contains");
                        expr = Expression.Call(left, containsMethod, right);
                    }
                    else
                    {
                        var containsMethod = typeof(Enumerable).GetMethods().First(x => x.Name == "Contains" && x.GetParameters().Count() == 2).MakeGenericMethod(new Type[] { objValue.GetType() });
                        expr = Expression.Call(containsMethod, left, right);
                    }

                    //where(x=> x.Tags.Contains(y))
                    binaryOp = Expression.Equal(expr, Expression.Constant(true));
                }
                else
                    binaryOp = Expression.LessThanOrEqual(left, right);

                var delegateType = typeof(Func<,>).MakeGenericType(elementType, typeof(bool));

                //Construct Func<T, bool> = (x) => x.propName == value expression
                var lambda = Expression.Lambda(delegateType, binaryOp, paramX);

                //Find Queryable.Where(Expression<Func<TSource, bool>>) method
                var whereMethod = typeof(Queryable).GetMethods()
                 .Where(x => x.Name == "Where")
                 .Select(x => new { M = x, P = x.GetParameters() })
                 .Where(x => x.P.Length == 2
                             && x.P[0].ParameterType.IsGenericType
                             && x.P[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>)
                             && x.P[1].ParameterType.IsGenericType
                             && x.P[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>))
                 .Select(x => new { x.M, A = x.P[1].ParameterType.GetGenericArguments() })
                 .Where(x => x.A[0].IsGenericType
                             && x.A[0].GetGenericTypeDefinition() == typeof(Func<,>))
                 .Select(x => new { x.M, A = x.A[0].GetGenericArguments() })
                 .Where(x => x.A[0].IsGenericParameter
                             && x.A[1] == typeof(bool))
                 .Select(x => x.M)
                 .SingleOrDefault();

                retVal = whereMethod.MakeGenericMethod(elementType).Invoke(null, new object[] { queryable, lambda });

            }

            return retVal;
        }

        private static object ParseString(string str,Type typeToParse)
        {
            int intValue;
            double doubleValue;
            char charValue;
            bool boolValue;
            TimeSpan timespan;
            DateTime dateTime;

            if ((typeToParse == typeof(int) || typeToParse == typeof(int?)) && int.TryParse(str, out intValue))
                 return intValue;
            else if (typeToParse == typeof(double) && double.TryParse(str, out doubleValue))
                return doubleValue;
            else if (typeToParse == typeof(TimeSpan) && TimeSpan.TryParse(str, out timespan))
                return timespan;
            else if (typeToParse == typeof(DateTime) && DateTime.TryParse(str, out dateTime))
                return dateTime;
            else if (typeToParse == typeof(char) && char.TryParse(str, out charValue))
                return charValue;
            else if (typeToParse == typeof(bool) && bool.TryParse(str, out boolValue))
                return boolValue;

            return str;
        }
       
        private static Type GetEnumerableType(Type type)
        {
            return (from intType in type.GetInterfaces() where intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IEnumerable<>) select intType.GetGenericArguments()[0]).FirstOrDefault();
        }
    }

}

