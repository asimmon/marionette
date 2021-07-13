using System;
using System.Collections.Generic;

namespace Askaiser.UITesting
{
    internal static class DisposableExtensions
    {
        public static TOut ConvertAndDispose<TIn, TOut>(this TIn disposableObject, Func<TIn, TOut> converter)
            where TIn : IDisposable
        {
            if (disposableObject is null)
            {
                throw new ArgumentNullException(nameof(disposableObject));
            }

            var returningSameObject = false;

            try
            {
                var newObject = converter(disposableObject);
                returningSameObject = ReferenceEquals(disposableObject, newObject);
                return newObject;
            }
            finally
            {
                if (!returningSameObject)
                {
                    disposableObject.Dispose();
                }
            }
        }

        public static T ConvertAndDispose<T>(this T disposableObject, IEnumerable<Func<T, T>> converters)
            where T : IDisposable
        {
            if (disposableObject is null)
            {
                throw new ArgumentNullException(nameof(disposableObject));
            }

            var currentObject = disposableObject;

            foreach (var converter in converters)
            {
                var previousObject = currentObject;
                var isSameObject = false;

                try
                {
                    currentObject = converter(previousObject);
                    isSameObject = ReferenceEquals(previousObject, currentObject);
                }
                finally
                {
                    if (!isSameObject)
                    {
                        previousObject.Dispose();
                    }
                }
            }

            return currentObject;
        }
    }
}
