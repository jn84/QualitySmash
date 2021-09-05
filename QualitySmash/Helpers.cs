using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace QualitySmash
{
    class Helpers
    {
        // Store config values as a list of int within config
        internal static List<int> GetIdList(string toConvert, Dictionary<string, int> itemIdRef)
        {
            List<int> itemIds = new List<int>();

            string[] setToConvert = toConvert.Split(',');

            for (int i = 0; i < setToConvert.Length; i++)
            {
                setToConvert[i] = setToConvert[i].Trim();

                // Allow input to be item/category id or actual name
                if (!int.TryParse(setToConvert[i], out var outputValue))
                    outputValue = itemIdRef[setToConvert[i]];
                // TryGetValue

                itemIds.Add(outputValue);
            }

            return itemIds;
        }

        // Convert list of int back to human readable string of names
        internal static string GetNameString(List<int> toConvert, Dictionary<int, string> itemNameRef)
        {
            List<string> outputValues = new List<string>();

            foreach (int id in toConvert)
            {
                if (!itemNameRef.TryGetValue(id, out var outputValue))
                    continue;

                outputValues.Add(outputValue);
            }

            return string.Join(",", outputValues);
        }
    }
}
