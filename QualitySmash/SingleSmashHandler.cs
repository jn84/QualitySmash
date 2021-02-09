using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Logging;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

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

        internal void HandleClick(ButtonPressedEventArgs e)
        {
            var menu = modEntry.GetValidKeybindSmashMenu();

            if (menu == null || !config.EnableSingleItemSmashKeybinds)
                return;

            Item itemToSmash;

            ModEntry.SmashType smashType;
            if (modEntry.helper.Input.IsDown(config.ColorSmashKeybind))
                smashType = ModEntry.SmashType.Color;
            else if (modEntry.helper.Input.IsDown(config.QualitySmashKeybind))
                smashType = ModEntry.SmashType.Quality;
            else
                return;

            Game1.uiMode = true;
            var cursorPos = e.Cursor.GetScaledScreenPixels();
            Game1.uiMode = false;

            if (menu is ItemGrabMenu grabMenu)
            {
                itemToSmash = ScanForClickedItem(grabMenu.inventory.inventory, grabMenu.inventory.actualInventory, cursorPos, smashType);
                if (itemToSmash != null)
                {
                    DoSmash(itemToSmash, smashType);
                    return;
                }

                itemToSmash = ScanForClickedItem(grabMenu.ItemsToGrabMenu.inventory, grabMenu.ItemsToGrabMenu.actualInventory, cursorPos, smashType);
                if (itemToSmash != null)
                {
                    DoSmash(itemToSmash, smashType);
                    return;
                }
            }
            if (menu is GameMenu gameMenu)
            {
                if (!(gameMenu.GetCurrentPage() is InventoryPage inventoryPage))
                    return;

                itemToSmash = ScanForClickedItem(inventoryPage.inventory.inventory, inventoryPage.inventory.actualInventory, cursorPos, smashType);
                if (itemToSmash != null)
                {
                    DoSmash(itemToSmash, smashType);
                    return;
                }
            }
        }

        private Item ScanForClickedItem(List<ClickableComponent> clickableItems, IList<Item> actualItems, Vector2 cursorPos, ModEntry.SmashType smashType)
        {
            foreach (var clickableItem in clickableItems)
            {
                if (!clickableItem.containsPoint((int)cursorPos.X, (int)cursorPos.Y))
                    continue;

                var itemSlotNumber = Convert.ToInt32(clickableItem.name);

                if (itemSlotNumber < actualItems.Count &&
                    actualItems[itemSlotNumber] != null)
                {
                    return actualItems[itemSlotNumber];
                }
            }
            return null;
        }

        private void DoSmash(Item item, ModEntry.SmashType smashType)
        {
            if (item.maximumStackSize() <= 1)
                return;

            if (smashType == ModEntry.SmashType.Color)
            {
                if (item.category == -80 && item is ColoredObject c)
                    c.color.Value = default;
            }

            if (smashType == ModEntry.SmashType.Quality)
            {
                if (item is StardewValley.Object o && o.Quality != 0)
                    o.Quality /= 2;
            }
        }

        // Should be reworked to hover over any item in any inventory
        internal bool TryHover(float x, float y)
        {
            this.hoverTextColor = "Smash Color";
            this.hoverTextQuality = "Smash Quality";
            var menu = modEntry.GetValidKeybindSmashMenu();

            if (menu == null || !config.EnableSingleItemSmashKeybinds)
                return false;

            
            // Modify the default hover text if the correct key isHeld()
            
            return false;
        }
    }
}
