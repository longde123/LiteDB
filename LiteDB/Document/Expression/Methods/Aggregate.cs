﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal partial class BsonExpressionMethods
    {
        /// <summary>
        /// Count all values. Return a single value
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> COUNT(IEnumerable<BsonValue> values)
        {
            yield return values.Count();
        }

        /// <summary>
        /// Find minimal value from all values (number values only). Return a single value
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> MIN(IEnumerable<BsonValue> values)
        {
            var min = BsonValue.MaxValue;

            foreach(var value in values)
            {
                if (value.CompareTo(min) <= 0)
                {
                    min = value;
                }
            }

            yield return min == BsonValue.MaxValue ? BsonValue.MinValue : min;
        }

        /// <summary>
        /// Find max value from all values (number values only). Return a single value
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> MAX(IEnumerable<BsonValue> values)
        {
            var max = BsonValue.MinValue;

            foreach (var value in values)
            {
                if (value.CompareTo(max) >= 0)
                {
                    max = value;
                }
            }

            yield return max == BsonValue.MinValue ? BsonValue.MaxValue : max;
        }

        /// <summary>
        /// Returns first value from an list of values
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> FIRST(IEnumerable<BsonValue> values)
        {
            yield return values.FirstOrDefault();
        }

        /// <summary>
        /// Returns last value from an list of values
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> LAST(IEnumerable<BsonValue> values)
        {
            yield return values.LastOrDefault();
        }

        /// <summary>
        /// Find average value from all values (number values only). Return a single value
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> AVG(IEnumerable<BsonValue> values)
        {
            var sum = new BsonValue(0);
            var count = 0;

            foreach (var value in values.Where(x => x.IsNumber))
            {
                sum += value;
                count++;
            }

            if (count > 0)
            {
                yield return sum / count;
            }
        }

        /// <summary>
        /// Sum all values (number values only). Return a single value
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> SUM(IEnumerable<BsonValue> values)
        {
            var sum = new BsonValue(0);

            foreach (var value in values.Where(x => x.IsNumber))
            {
                sum += value;
            }

            yield return sum;
        }

        /// <summary>
        /// Return "true" only if all values are true
        /// ALL($.items[*] > 0)
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> ALL(IEnumerable<BsonValue> values)
        {
            yield return values
                .Where(x => x.IsBoolean)
                .Select(x => x.AsBoolean)
                .All(x => x == true);
        }

        /// <summary>
        /// Return "true" if any values are true
        /// ANY($._id = ITEMS([1, 2, 3, 4]))
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> ANY(IEnumerable<BsonValue> values)
        {
            yield return values
                .Where(x => x.IsBoolean)
                .Select(x => x.AsBoolean)
                .Any(x => x == true);
        }

        /// <summary>
        /// Join all values into a single string with ',' separator. Return a single value
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> JOIN(IEnumerable<BsonValue> values)
        {
            return JOIN(values, null);
        }

        /// <summary>
        /// Join all values into a single string with a string separator. Return a single value
        /// </summary>
        [Aggregate]
        public static IEnumerable<BsonValue> JOIN(IEnumerable<BsonValue> values, IEnumerable<BsonValue> separator = null)
        {
            yield return string.Join(
                separator?.FirstOrDefault().AsString ?? ",",
                values.Select(x => x.AsString).ToArray()
            );
        }
    }
}
