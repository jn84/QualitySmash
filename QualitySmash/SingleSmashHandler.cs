using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Logging;
using StardewValley;
using StardewValley.Menus;

namespace QualitySmash
{
    internal class SingleSmashHandler
    {
        private string hoverTextColor;
        private string hoverTextQuality;
        private readonly ModEntry modEntry;
        private readonly ModConfig config;

        public SingleSmashHandler(ModEntry modEntry, ModConfig config)
        {
            this.modEntry = modEntry;
            this.config = config;
        }

        internal IClickableMenu GetActiveInventoryMenu()
        {
            // InventoryMenu or MenuWithInventory
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is MenuWithInventory)
            {
                var menu = Game1.activeClickableMenu as MenuWithInventory;
                if (menu is ItemGrabMenu grabMenu)
                {
                    // Exclude mini shipping bin, shipping bin, fishing chests, etc
                    if (grabMenu.ItemsToGrabMenu.capacity <= 9 || grabMenu.source == 2 || grabMenu.source == 3)
                        return null;
                    return grabMenu;
                }
            }
            return null;
        }

        // Should be reworked to hover over any item in any inventory
        internal bool TryHover(float x, float y)
        {
            this.hoverTextColor = "";
            this.hoverTextQuality = "";
            var menu = GetActiveInventoryMenu();

            if (menu != null)
            {

            }
            return false;
        }
    }
}
