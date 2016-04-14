// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AzureCat.Samples.ObserverPattern.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json.Linq;

    public class ExpressionBuilder<T>
    {
        #region Public Constructor

        public ExpressionBuilder()
        {
            this.type = typeof(T);
            this.propertyList = new List<PropertyInfo>(this.type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        #endregion

        private Expression GetExpression(ParameterExpression param, ExpressionFilter filter)
        {
            if (param == null)
            {
                throw new ArgumentNullException(nameof(param));
            }
            if (this.type != typeof(JObject))
            {
                PropertyInfo propertyInfo =
                    this.propertyList.FirstOrDefault(p => string.Compare(p.Name, filter.Property, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (propertyInfo != null)
                {
                    MemberExpression member = Expression.Property(param, filter.Property);
                    ConstantExpression constant = filter.Value != null
                        ? Expression.Constant(filter.Value, Nullable.GetUnderlyingType(filter.Value.GetType()) ?? filter.Value.GetType())
                        : Expression.Constant(filter.Value);
                    Expression left = propertyInfo.PropertyType != typeof(DateTime?) ? member : (Expression) Expression.Call(member, this.getValueOrDefault);

                    switch (filter.Operator)
                    {
                        case Operator.Equal:
                            return Expression.Equal(left, constant);
                        case Operator.NotEqual:
                            return Expression.NotEqual(left, constant);
                        case Operator.GreaterThan:
                            return Expression.GreaterThan(left, constant);
                        case Operator.GreaterThanOrEqual:
                            return Expression.GreaterThanOrEqual(left, constant);
                        case Operator.LessThan:
                            return Expression.LessThan(left, constant);
                        case Operator.LessThanOrEqual:
                            return Expression.LessThanOrEqual(left, constant);
                        case Operator.Contains:
                            return Expression.Call(left, this.containsMethod, constant);
                        case Operator.StartsWith:
                            return Expression.Call(left, this.startsWithMethod, constant);
                        case Operator.EndsWith:
                            return Expression.Call(left, this.endsWithMethod, constant);
                    }
                }
            }
            else
            {
                IndexExpression member = Expression.Property(
                    param,
                    typeof(JObject).GetProperty("Item", new[] {typeof(string)}),
                    Expression.Constant(filter.Property, typeof(string)));
                //var value = Expression.Call(member, typeof (JToken).GetMethod("Value").MakeGenericMethod(typeof(string)));

                ConstantExpression constant = filter.Value != null
                    ? Expression.Constant(filter.Value, Nullable.GetUnderlyingType(filter.Value.GetType()) ?? filter.Value.GetType())
                    : Expression.Constant(filter.Value);
                MethodInfo changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new[] {typeof(object), typeof(Type)});
                Expression value = Expression.Call(changeTypeMethod, member, Expression.Constant(typeof(string)));
                    //Expression.Call(member, typeof(JToken).GetMethod("ToString", new Type[] { }));
                value = Expression.Convert(value, typeof(string));
                if (filter.Value is double)
                {
                    MethodInfo parseMethod = typeof(double).GetMethod("Parse", new[] {typeof(string)});
                    value = Expression.Call(parseMethod, value);
                }
                Expression left = value;

                switch (filter.Operator)
                {
                    case Operator.Equal:
                        return Expression.Equal(left, constant);
                    case Operator.NotEqual:
                        return Expression.NotEqual(left, constant);
                    case Operator.GreaterThan:
                        return Expression.GreaterThan(left, constant);
                    case Operator.GreaterThanOrEqual:
                        return Expression.GreaterThanOrEqual(left, constant);
                    case Operator.LessThan:
                        return Expression.LessThan(left, constant);
                    case Operator.LessThanOrEqual:
                        return Expression.LessThanOrEqual(left, constant);
                    case Operator.Contains:
                        return Expression.Call(left, this.containsMethod, constant);
                    case Operator.StartsWith:
                        return Expression.Call(left, this.startsWithMethod, constant);
                    case Operator.EndsWith:
                        return Expression.Call(left, this.endsWithMethod, constant);
                }
            }
            return null;
        }

        #region Private Constants

        //***************************
        // Constants
        //***************************
        private const string PropertyGroup = "Property";
        private const string OperatorGroup = "Operator";
        private const string ValueGroup = "Value";
        private const string AndPattern = @"(?i)\s+(and|or)\s+";
        private const string PropertyPattern = @"(?i)(?<Property>(\w|\.)+)\s*(?<Operator>=|!=|>=?|<=?|contains|startswith|endswith)\s*(?<Value>\S+)";
        private const string OpArgument = "op";
        private const string ValueArgument = "value";
        private const string FilterExpressionArgument = "filterExpression";
        private const string FilterListArgument = "filterList";
        private const string ParamArgument = "param";
        private const string Filter1Argument = "filter1";
        private const string Filter2Argument = "filter2";
        private const string NullValue = "null";
        private const string AndOperator = "and";
        private const string OrOperator = "or";

        //***************************
        // Messages
        //***************************
        private const string ArgumentCannotBeNull = "The argument [{0}] cannot be null.";
        private const string ArgumentCannotBeNullOrEmpty = "The argument [{0}] cannot be null or empty.";
        private const string CollectionCannotBeNullOrEmpty = "The collection [{0}] cannot be null or empty.";
        private const string StringNotDelimited = "The string [{0}] is not properly delimited by ' or \" characters.";
        private const string PropertyDoesNotExist = "The type [{0}] does not contain the [{1}] property.";
        private const string PropertyIsNotPrimitiveOrDateTimeOrString = "The type [{0}] of the [{1}] property is not primitive, datetime or string.";
        private const string OperatorUnknownAndUnsupported = "The operator [{0}] is unknown and unsupported.";

        private const string ErrorConvertingValue =
            "An error occurred while converting the string [{0}] to [{1}] type. See the inner exception for more information.";

        private const string PredicateInvalid = "The predicate [{0}] is invalid.";
        private const string FilterExpressionCannotBeNullOrEmpty = "The filter expression cannot be null or empty.";
        private const string FilterExpressionCannotStartWithLogicalOperator = "The filter expression [{0}] cannot start with a logical operator [AND, OR].";

        #endregion

        #region Private Instance Fields

        private readonly MethodInfo containsMethod = typeof(string).GetMethod("Contains");
        private readonly MethodInfo startsWithMethod = typeof(string).GetMethod("StartsWith", new[] {typeof(string)});
        private readonly MethodInfo endsWithMethod = typeof(string).GetMethod("EndsWith", new[] {typeof(string)});
        private readonly MethodInfo getValueOrDefault = typeof(DateTime?).GetMethod("GetValueOrDefault", new Type[] {});
        private readonly List<PropertyInfo> propertyList;
        private readonly Type type;

        #endregion

        #region Public Properties

        public Expression<Func<T, bool>> GetExpression(string filterExpression)
        {
            if (string.IsNullOrWhiteSpace(filterExpression))
            {
                throw new ArgumentException(FilterExpressionCannotBeNullOrEmpty, FilterExpressionArgument);
            }
            List<ExpressionFilter> filterList = new List<ExpressionFilter>();
            string[] predicates = Regex.Split(filterExpression, AndPattern, RegexOptions.IgnoreCase);
            Regex propertyRegex = new Regex(PropertyPattern);
            foreach (string predicate in predicates)
            {
                if (string.Compare(predicate, AndOperator, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    if (filterList.Count == 0)
                    {
                        throw new ApplicationException(FilterExpressionCannotStartWithLogicalOperator);
                    }
                    filterList[filterList.Count - 1].LogicalOperator = LogicalOperator.And;
                    continue;
                }
                if (string.Compare(predicate, OrOperator, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    if (filterList.Count == 0)
                    {
                        throw new ApplicationException(FilterExpressionCannotStartWithLogicalOperator);
                    }
                    filterList[filterList.Count - 1].LogicalOperator = LogicalOperator.Or;
                    continue;
                }
                MatchCollection matches = propertyRegex.Matches(predicate);
                for (int j = 0; j < matches.Count; j++)
                {
                    string property = matches[j].Groups[PropertyGroup].Value;
                    string op = matches[j].Groups[OperatorGroup].Value;
                    string value = matches[j].Groups[ValueGroup].Value;
                    Operator operatorInfo = GetOperator(op);

                    if (string.Compare(value, "null", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        value = null;
                    }

                    if (this.type != typeof(JObject))
                    {
                        PropertyInfo propertyInfo =
                            this.propertyList.FirstOrDefault(p => string.Compare(p.Name, property, StringComparison.InvariantCultureIgnoreCase) == 0);

                        if (string.IsNullOrWhiteSpace(property) ||
                            string.IsNullOrWhiteSpace(op) ||
                            string.IsNullOrWhiteSpace(value))
                        {
                            throw new ApplicationException(string.Format(PredicateInvalid, predicate));
                        }

                        if (propertyInfo == null)
                        {
                            throw new ApplicationException(string.Format(PropertyDoesNotExist, typeof(T).Name, property));
                        }

                        if (!propertyInfo.PropertyType.IsPrimitive &&
                            propertyInfo.PropertyType != typeof(string) &&
                            propertyInfo.PropertyType != typeof(DateTime) &&
                            propertyInfo.PropertyType != typeof(DateTime?))
                        {
                            throw new ApplicationException(
                                string.Format(PropertyIsNotPrimitiveOrDateTimeOrString, propertyInfo.PropertyType, propertyInfo.Name));
                        }

                        if (operatorInfo == Operator.Unkwnon)
                        {
                            throw new ApplicationException(string.Format(OperatorUnknownAndUnsupported, op));
                        }

                        object typedValue;
                        try
                        {
                            typedValue = propertyInfo.PropertyType == typeof(string)
                                ? GetString(value)
                                : propertyInfo.PropertyType == typeof(DateTime) ||
                                  propertyInfo.PropertyType == typeof(DateTime?)
                                    ? Convert.ChangeType(
                                        RemoveDelimiters(value),
                                        Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType)
                                    : Convert.ChangeType(value, propertyInfo.PropertyType);
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException(string.Format(ErrorConvertingValue, value, propertyInfo.PropertyType.Name), ex);
                        }

                        filterList.Add(
                            new ExpressionFilter
                            {
                                Property = propertyInfo.Name,
                                Operator = operatorInfo,
                                Value = typedValue
                            });
                    }
                    else
                    {
                        if ((value?[0] == '\'' && value[value.Length - 1] == '\'') ||
                            (value?[0] == '"' && value[value.Length - 1] == '"'))
                        {
                            value = value.Substring(1, value.Length - 2);
                            filterList.Add(
                                new ExpressionFilter
                                {
                                    Property = property,
                                    Operator = operatorInfo,
                                    Value = value
                                });
                        }
                        else
                        {
                            double typedValue;
                            if (double.TryParse(value, out typedValue))
                            {
                                filterList.Add(
                                    new ExpressionFilter
                                    {
                                        Property = property,
                                        Operator = operatorInfo,
                                        Value = typedValue
                                    });
                            }
                            else
                            {
                                filterList.Add(
                                    new ExpressionFilter
                                    {
                                        Property = property,
                                        Operator = operatorInfo,
                                        Value = value
                                    });
                            }
                        }
                    }
                }
            }
            return this.GetExpression(filterList);
        }

        public Expression<Func<T, bool>> GetExpression(IList<ExpressionFilter> filterList)
        {
            if (filterList.Count == 0)
            {
                throw new ArgumentException(string.Format(CollectionCannotBeNullOrEmpty, FilterListArgument), FilterListArgument);
            }

            ParameterExpression param = Expression.Parameter(typeof(T), "t");
            Expression exp = null;
            LogicalOperator op = LogicalOperator.And;
            switch (filterList.Count)
            {
                case 1:
                    exp = this.GetExpression(param, filterList[0]);
                    break;
                case 2:
                    exp = this.GetExpression(param, filterList[0], filterList[1]);
                    break;
                default:
                    while (filterList.Count > 0)
                    {
                        ExpressionFilter f1 = filterList[0];
                        ExpressionFilter f2 = filterList[1];

                        exp = exp == null
                            ? this.GetExpression(param, filterList[0], filterList[1])
                            : op == LogicalOperator.Or
                                ? Expression.OrElse(exp, this.GetExpression(param, filterList[0], filterList[1]))
                                : Expression.AndAlso(exp, this.GetExpression(param, filterList[0], filterList[1]));

                        op = filterList[1].LogicalOperator;

                        filterList.Remove(f1);
                        filterList.Remove(f2);

                        if (filterList.Count != 1)
                        {
                            continue;
                        }
                        exp = Expression.AndAlso(exp, this.GetExpression(param, filterList[0]));
                        filterList.RemoveAt(0);
                    }
                    break;
            }

            return exp != null ? Expression.Lambda<Func<T, bool>>(exp, param) : null;
        }

        #endregion

        #region Private Instance Methods

        private BinaryExpression GetExpression(ParameterExpression param, ExpressionFilter filter1, ExpressionFilter filter2)
        {
            if (param == null)
            {
                throw new ArgumentNullException(ParamArgument, string.Format(ArgumentCannotBeNull, ParamArgument));
            }
            if (filter1 == null)
            {
                throw new ArgumentNullException(Filter1Argument, string.Format(ArgumentCannotBeNull, Filter1Argument));
            }
            if (filter2 == null)
            {
                throw new ArgumentNullException(Filter2Argument, string.Format(ArgumentCannotBeNull, Filter2Argument));
            }
            Expression bin1 = this.GetExpression(param, filter1);
            Expression bin2 = this.GetExpression(param, filter2);
            return filter1.LogicalOperator == LogicalOperator.Or
                ? Expression.OrElse(bin1, bin2)
                : Expression.AndAlso(bin1, bin2);
        }

        private static Operator GetOperator(string op)
        {
            if (string.IsNullOrWhiteSpace(op))
            {
                throw new ArgumentException(string.Format(ArgumentCannotBeNullOrEmpty, OpArgument), OpArgument);
            }
            switch (op.ToLower())
            {
                case "=":
                    return Operator.Equal;
                case "!=":
                    return Operator.NotEqual;
                case ">":
                    return Operator.GreaterThan;
                case ">=":
                    return Operator.GreaterThanOrEqual;
                case "<":
                    return Operator.LessThan;
                case "<=":
                    return Operator.LessThanOrEqual;
                case "contains":
                    return Operator.Contains;
                case "startswith":
                    return Operator.StartsWith;
                case "endswith":
                    return Operator.EndsWith;
                default:
                    return Operator.Unkwnon;
            }
        }

        #endregion

        #region Private Static Methods

        private static string GetString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(string.Format(ArgumentCannotBeNullOrEmpty, ValueArgument), ValueArgument);
            }
            if (string.Compare(value, NullValue, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return null;
            }
            if (value.Length >= 2 &&
                (value[0] == '\'' && value[value.Length - 1] == '\'') ||
                (value[0] == '"' && value[value.Length - 1] == '"'))
            {
                return value.Length == 2 ? string.Empty : value.Substring(1, value.Length - 2);
            }
            throw new ApplicationException(string.Format(StringNotDelimited, value));
        }

        private static string RemoveDelimiters(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(string.Format(ArgumentCannotBeNullOrEmpty, ValueArgument), ValueArgument);
            }
            if (value.Length >= 2 &&
                (value[0] == '\'' && value[value.Length - 1] == '\'') ||
                (value[0] == '"' && value[value.Length - 1] == '"'))
            {
                return value.Length == 2 ? string.Empty : value.Substring(1, value.Length - 2);
            }
            return value;
        }

        #endregion
    }
}