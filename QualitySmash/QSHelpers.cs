using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace QualitySmash
{
    class QSHelpers
    {
        internal static void SyncConfigSetting(bool value, int id, List<int> configList)
        {
            if (value)
            {
                if (configList.Contains(id))
                    return;
                else
                    configList.Add(id);
            }
            else
            {
                if (configList.Contains(id))
                    configList.Remove(id);
            }

        }
    }
}
