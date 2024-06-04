using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VantageConnectorService.Helpers
{
    public static class ContainerParser
    {
        public static ICollection<string> Parse(string text)
        {
            
            try
            {
                if (text.Equals("all", StringComparison.InvariantCultureIgnoreCase))
                    return new List<string>();

                char delimiter = ',';
                var substrings = text.Split(delimiter).Where(s => !string.IsNullOrEmpty(s));
                return substrings.Select(s => s.Trim()).ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("Container Parsing Error: " + ex.Message);
            }

        }

    }
}
