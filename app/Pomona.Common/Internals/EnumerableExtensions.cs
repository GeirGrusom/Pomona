﻿// ----------------------------------------------------------------------------
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pomona.Common.Internals
{
    public static class EnumerableExtensions
    {
        public static void AddTo<T>(this IEnumerable<T> source, ICollection<T> target)
        {
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, params T[] value)
        {
            return source.Concat((IEnumerable<T>) value);
        }

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        public static IQueryable<T> EmptyIfNull<T>(this IQueryable<T> source)
        {
            return source ?? Enumerable.Empty<T>().AsQueryable();
        }

        public static IEnumerable<T> WalkTree<T>(this T o, Func<T, T> nextNodeSelector)
            where T : class
        {
            while (o != null)
            {
                yield return o;
                o = nextNodeSelector(o);
            }
        }

        public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            foreach (var item in source)
            {
                yield return item;
                if (predicate(item))
                    yield break;
            }
        }
    }
}