﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2013 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Pomona.Common.Internals
{
    public static class StringConversion
    {
        private static readonly Type[] floatTypes =
        {
            typeof(Single), typeof(Double), typeof(Decimal)
        };

        private static readonly Type[] intTypes =
        {
            typeof(Byte), typeof(SByte), typeof(Int16), typeof(UInt16),
            typeof(Int32), typeof(UInt32), typeof(Int64), typeof(UInt64)
        };

        private static ConcurrentDictionary<Type, IStringConverter> converterCache =
            new ConcurrentDictionary<Type, IStringConverter>();


        public static bool TryParse(this string s, Type toType, IFormatProvider provider, out object result)
        {
            if (toType == null)
                throw new ArgumentNullException("toType");

            return GetCachedConverter(toType).TryParse(s, provider, out result);
        }


        public static bool TryParse(this string s, Type toType, out object result)
        {
            return GetCachedConverter(toType).TryParse(s, out result);
        }


        public static bool TryParse<T>(this string s, out T value)
        {
            return StringConverter<T>.TryParse(s, out value);
        }


        public static bool TryParse<T>(this string s, IFormatProvider provider, out T value)
        {
            return StringConverter<T>.TryParse(s, provider, out value);
        }


        private static IStringConverter GetCachedConverter(Type toType)
        {
            return converterCache.GetOrAdd(toType,
                t => (IStringConverter)Activator.CreateInstance(typeof(StringConverter<>).MakeGenericType(toType)));
        }

        #region Nested type: IStringConverter

        internal interface IStringConverter
        {
            bool TryParse(string s, out object result);
            bool TryParse(string s, IFormatProvider provider, out object result);
        }

        #endregion

        #region Nested type: StringConverter

        internal class StringConverter<T> : IStringConverter
        {
            private static readonly NumberStyles defaultNumberStyle;
            private static readonly TryParseDelegate tryParseMethod;
            private static readonly TryParseWithNumberStyleDelegate tryParseWithNumberStyleMethod;


            static StringConverter()
            {
                var tryParseMethods = typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(x => x.Name == "TryParse" && x.ReturnType == typeof(bool)).ToArray();

                tryParseWithNumberStyleMethod = RecognizeTryParseWithNumberStyleMethod(tryParseMethods,
                    out defaultNumberStyle);
                tryParseMethod = RecognizeTryParseWithNoExtraArguments(tryParseMethods);
            }


            public static bool TryParse(string s, out T result)
            {
                if (tryParseMethod != null)
                    return tryParseMethod(s, out result);
                if (tryParseWithNumberStyleMethod != null)
                    return tryParseWithNumberStyleMethod(s, defaultNumberStyle, NumberFormatInfo.CurrentInfo, out result);
                try
                {
                    result = (T)Convert.ChangeType(s, typeof(T));
                    return true;
                }
                catch (Exception)
                {
                    result = default(T);
                    return false;
                }
            }


            public static bool TryParse(string s, IFormatProvider provider, out T result)
            {
                if (tryParseWithNumberStyleMethod != null)
                    return tryParseWithNumberStyleMethod(s, defaultNumberStyle, provider, out result);
                if (tryParseMethod != null)
                    return tryParseMethod(s, out result);
                try
                {
                    result = (T)Convert.ChangeType(s, typeof(T), provider);
                    return true;
                }
                catch (Exception)
                {
                    result = default(T);
                    return false;
                }
            }


            private static TryParseDelegate RecognizeTryParseWithNoExtraArguments(
                IEnumerable<MethodInfo> tryParseMethods)
            {
                var tByRef = typeof(T).MakeByRefType();
                if (typeof(T) == typeof(string))
                {
                    return (TryParseDelegate)Delegate.CreateDelegate(typeof(TryParseDelegate),
                        typeof(StringConverter<T>),
                        "TryParseString");
                }

                var parseMethod = tryParseMethods
                    .Select(x => new { parameters = x.GetParameters(), info = x }).Where(
                        x => x.parameters.Length == 2 &&
                             x.parameters[0].ParameterType == typeof(string)
                             && x.parameters[1].ParameterType == tByRef)
                    .Select(x => x.info).FirstOrDefault();

                if (parseMethod != null)
                {
                    var sParam = Expression.Parameter(typeof(string), "s");
                    var resultParam = Expression.Parameter(tByRef, "result");
                    return Expression.Lambda<TryParseDelegate>(
                        Expression.Call(parseMethod, new Expression[] { sParam, resultParam }),
                        sParam,
                        resultParam).Compile();
                }
                return null;
            }


            private static TryParseWithNumberStyleDelegate RecognizeTryParseWithNumberStyleMethod(
                IEnumerable<MethodInfo> tryParseMethods,
                out NumberStyles defaultNumberStyle)
            {
                var tByRef = typeof(T).MakeByRefType();
                defaultNumberStyle = default(NumberStyles);

                var parseMethod = tryParseMethods
                    .Select(x => new { parameters = x.GetParameters(), info = x }).Where(
                        x => x.parameters.Length == 4 &&
                             x.parameters[0].ParameterType == typeof(string)
                             && x.parameters[1].ParameterType == typeof(NumberStyles)
                             && x.parameters[2].ParameterType == typeof(IFormatProvider)
                             && x.parameters[3].ParameterType == tByRef)
                    .Select(x => x.info).FirstOrDefault();

                if (parseMethod != null)
                {
                    var sParam = Expression.Parameter(typeof(string), "s");
                    var styleParam = Expression.Parameter(typeof(NumberStyles), "style");
                    var providerParam = Expression.Parameter(typeof(IFormatProvider), "provider");
                    var resultParam = Expression.Parameter(tByRef, "result");

                    if (intTypes.Contains(typeof(T)))
                        defaultNumberStyle = NumberStyles.Integer;
                    if (floatTypes.Contains(typeof(T)))
                        defaultNumberStyle = NumberStyles.Float | NumberStyles.AllowThousands;

                    return Expression.Lambda<TryParseWithNumberStyleDelegate>(
                        Expression.Call(parseMethod, new Expression[] { sParam, styleParam, providerParam, resultParam }),
                        sParam,
                        styleParam,
                        providerParam,
                        resultParam).Compile();
                }
                return null;
            }


            private static bool TryParseString(string s, out string result)
            {
                result = s;
                return true;
            }


            bool IStringConverter.TryParse(string s, out object result)
            {
                result = null;
                T typedResult;
                if (!TryParse(s, out typedResult))
                    return false;
                result = typedResult;
                return true;
            }


            bool IStringConverter.TryParse(string s, IFormatProvider provider, out object result)
            {
                result = null;
                T typedResult;
                if (!TryParse(s, provider, out typedResult))
                    return false;
                result = typedResult;
                return true;
            }

            #region Nested type: TryParseDelegate

            private delegate bool TryParseDelegate(string s, out T result);

            #endregion

            #region Nested type: TryParseWithNumberStyleDelegate

            private delegate bool TryParseWithNumberStyleDelegate(
                string s,
                NumberStyles style,
                IFormatProvider provider,
                out T result);

            #endregion
        }

        #endregion
    }
}