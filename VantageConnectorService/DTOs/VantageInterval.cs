using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VantageConnectorService.DTOs
{
    public class VantageInterval
    {
        public bool IsDaily { get; set; }
        public int SyncHour { get; set; }
        public int SyncMinute { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; }
    }
}
