using System;

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
    }
}