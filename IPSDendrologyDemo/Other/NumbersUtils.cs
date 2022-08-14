using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSDendrologyDemo.Other
{
    public class NumbersUtils
    {
        public static int ParseStringToInt(string stringToParse)
        {
            stringToParse = stringToParse.Replace(',', '.');
            if (int.TryParse(stringToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }
    }
}
