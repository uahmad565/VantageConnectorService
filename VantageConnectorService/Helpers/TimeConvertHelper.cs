using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VantageConnectorService.Helpers
{
    public class TimeConvertHelper
    {
        public static int ToMilliseconds(int minutes)
        {
            return minutes * 60 * 1000;


        }
    }
}
