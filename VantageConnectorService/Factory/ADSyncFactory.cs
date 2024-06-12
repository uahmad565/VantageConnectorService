using ActiveDirectorySearcher.DTOs;
using VantageConnectorService.DTOs;
using VantageConnectorService.GlobalObjects;
using VantageConnectorService.Helpers;

namespace VantageConnectorService.Factory
{
    internal class ADSyncFactory
    {
        public static ADSync Create(SettingData settingData, VantageConfig config, bool ignoreVantageInterval = false)
        {
            List<string> oUlist = new List<string>();
            if (!string.IsNullOrEmpty(settingData.selectedOUs))
                oUlist = ContainerParser.Parse(settingData.selectedOUs).ToList();
            InputCreds input = new InputCreds(settingData.domainName, settingData.domainUserName, settingData.password, settingData.port, config.domainId, config.host);
            VantageInterval? vantageInterval = null;
            if (!ignoreVantageInterval)
                vantageInterval = PrepareVantageInterval(settingData);
            List<ObjectType> objectTypes = PrepareObjectTypes(settingData.dataType);
            int recordsToSyncInSingleRequest = settingData.recordsToSyncInSingleRequest == 0 ? 200 : settingData.recordsToSyncInSingleRequest;
            return new ADSync(input, oUlist, vantageInterval, objectTypes, recordsToSyncInSingleRequest);
        }

        private static List<ObjectType> PrepareObjectTypes(string[] datatype)
        {
            List<ObjectType> list = new List<ObjectType>();
            foreach (var item in datatype)
            {
                if (Enum.TryParse(typeof(ObjectType), item, true, out var result))
                {
                    if (result != null)
                        list.Add((ObjectType)result);
                }
                else
                {
                    GlobalLogManager.Logger.Warn($"Ignore invalid enum value: {item} in Preparing Object Types for ActiveDirectorySearcher");
                }
            }
            return list;
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
