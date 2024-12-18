using System;

namespace Multiformats.Address;

internal static class Extensions
{
    public static T[] Slice<T>(this T[] array, int offset, int? count = null)
    {
        var result = new T[count ?? array.Length - offset];
        Array.Copy(array, offset, result, 0, result.Length);
        return result;
    }
}
