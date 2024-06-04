using ActiveDirectorySearcher.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VantageConnectorService.DTOs;
using VantageConnectorService.Helpers;

namespace VantageConnectorService.Factory
{
    internal class ADSyncFactory
    {
        public static ADSync Create(SettingData settingData, VantageConfig config)
        {
            List<string> oUlist = new List<string>();
            if (!string.IsNullOrEmpty(settingData.selectedOUs))
                oUlist = ContainerParser.Parse(settingData.selectedOUs).ToList();
            InputCreds input = new InputCreds(settingData.domainName, settingData.domainUserName, settingData.password, settingData.port, config.domainId, config.host);
            VantageInterval vantageInterval = PrepareVantageInterval(settingData);
            return new ADSync(input, oUlist, vantageInterval);
        }

        private static VantageInterval PrepareVantageInterval(SettingData settingData)
        {
            List<DayOfWeek> daysOfWeek = new List<DayOfWeek>();
            bool isDaily = settingData.syncFrequency.Equals("Daily", StringComparison.InvariantCultureIgnoreCase) ? true : false;
            if (!isDaily)
            {
                if (settingData.syncDays.monday)
                    daysOfWeek.Add(DayOfWeek.Monday);

                if (settingData.syncDays.tuesday)
                    daysOfWeek.Add(DayOfWeek.Tuesday);

                if (settingData.syncDays.wednesday)
                    daysOfWeek.Add(DayOfWeek.Wednesday);

                if (settingData.syncDays.thursday)
                    daysOfWeek.Add(DayOfWeek.Thursday);

                if (settingData.syncDays.friday)
                    daysOfWeek.Add(DayOfWeek.Friday);

                if (settingData.syncDays.saturday)
                    daysOfWeek.Add(DayOfWeek.Saturday);

                if (settingData.syncDays.sunday)
                    daysOfWeek.Add(DayOfWeek.Sunday);
            }

            return new VantageInterval()
            {
                SyncHour = settingData.syncHours,
                SyncMinute = settingData.syncMinutes,
                IsDaily = isDaily,
                DaysOfWeek = daysOfWeek
            };
        }
    }
}
