using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accelera.Models
{
    public static class Extension
    {
        /// <summary>
        /// Poulate a array with a given value. 
        /// i++: copies i, increments i, and returns the original value. 
        /// ++i just returns the incremented value. 
        /// Therefore one can assume that ++i is faster but due to compiler optimization this holds not true.
        /// The slowest possible way would be "Enumerable.Repeat(true, 1000000).ToArray();"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="value"></param>
        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }
    }
}
