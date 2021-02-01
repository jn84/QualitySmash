using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections.Generic;

namespace QualitySmash
{
    class QualitySmashHandler
    {
        private string hoverTextColor;
        private string hoverTextQuality;
        private readonly ModEntry modEntry;
        private readonly ClickableTextureComponent buttonColor;
        private readonly ClickableTextureComponent buttonQuality;
        private readonly Texture2D imageColor;
        private readonly Texture2D imageQuality;
        private readonly ModConfig config;

        /// <summary>
        /// Initializes stuff for the mod.
        /// </summary>
        /// <param name="modEntry"></param>
        /// <param name="config"></param>
        /// <param name="imageColor"></param>
        /// <param name="imageQuality"></param>
        public QualitySmashHandler(ModEntry modEntry, ModConfig config, Texture2D imageColor, Texture2D imageQuality)
        {
            this.modEntry = modEntry;
            this.config = config;
            this.imageColor = imageColor;
            this.imageQuality = imageQuality;
            buttonColor = new ClickableTextureComponent(Rectangle.Empty, null, new Rectangle(0, 0, 16, 16), 4f)
            {
                hoverText = modEntry.helper.Translation.Get("hoverTextColor")
            };

            buttonQuality = new ClickableTextureComponent(Rectangle.Empty, null, new Rectangle(0, 0, 16, 16), 4f)
            {
                hoverText = modEntry.helper.Translation.Get("hoverTextQuality")
            };
        }

        // TODO: Figure out where the buttons go
        private void UpdateButtonPositions()
        {
            var menu = Game1.activeClickableMenu;
            if (menu == null) return;

            var length = 16 * Game1.pixelZoom;
            const int positionFromBottom = 3;
            const int gapSize = 16;

            var screenX = menu.xPositionOnScreen + menu.width + gapSize + length;
            var screenY = menu.yPositionOnScreen + menu.height / 3 - (length * positionFromBottom) - (gapSize * (positionFromBottom - 1));

            buttonColor.bounds = new Rectangle(screenX, screenY, length, length);
            buttonQuality.bounds = new Rectangle(screenX, screenY + gapSize + length, length, length);
        }

        public void DrawButtons()
        {
            UpdateButtonPositions();

            buttonColor.texture = imageColor;
            buttonQuality.texture = imageQuality;

            buttonColor.draw(Game1.spriteBatch);
            buttonQuality.draw(Game1.spriteBatch);

            if (hoverTextColor != "")
                IClickableMenu.drawHoverText(Game1.spriteBatch, hoverTextColor, Game1.smallFont);

            if (hoverTextQuality != "")
                IClickableMenu.drawHoverText(Game1.spriteBatch, hoverTextQuality, Game1.smallFont);

            // Draws cursor over the GUI element
            Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()),
            Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16), Color.White, 0f, Vector2.Zero,
            4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 0);
        }

        internal bool TryHover(float x, float y)
        {
            this.hoverTextColor = "";
            this.hoverTextQuality = "";
            var menu = modEntry.GetContainerMenu();

            if (menu != null)
            { 
                buttonColor.tryHover((int)x, (int)y, 0.25f);

                if (buttonColor.containsPoint((int)x, (int)y))
                {
                    this.hoverTextColor = buttonColor.hoverText;
                    return true;
                }

                buttonQuality.tryHover((int)x, (int)y, 0.25f);

                if (buttonQuality.containsPoint((int)x, (int)y))
                {
                    this.hoverTextQuality = buttonQuality.hoverText;
                    return true;
                }
            }
            return false;
        }

        internal void HandleClick(ICursorPosition cursor)
        {
            var screenPixels = cursor.ScreenPixels;

            ItemGrabMenu menu = null;

            if (modEntry.GetContainerMenu() is ItemGrabMenu)
                menu = modEntry.GetContainerMenu() as ItemGrabMenu;

            if (menu == null)
                return;

            if (buttonColor.containsPoint((int) screenPixels.X, (int) screenPixels.Y))
            {
                Game1.playSound("clubhit");
                DoColorSmash(menu);
            }

            if (buttonQuality.containsPoint((int)screenPixels.X, (int)screenPixels.Y))
            {
                Game1.playSound("clubhit");
                DoQualitySmash(menu);
            }
        }


        //TODO: Need the container context
        private void DoColorSmash(ItemGrabMenu menu)
        {
            var areItemsChanged = false;

            var containerInventory = menu.ItemsToGrabMenu.actualInventory;

            var itemsProcessed = new List<Item>();

            modEntry.Monitor.Log(containerInventory.Count.ToString(), LogLevel.Info);
            for (var i = 0; i < containerInventory.Count; i++)
            {
                if (containerInventory[i] == null)
                    continue;
                

                // If not flower
                if (!(containerInventory[i] is ColoredObject c) || c.Category != -80) continue;

                areItemsChanged = true;
                
                c.color.Value = default;

                itemsProcessed.Add(containerInventory[i]);

                containerInventory.RemoveAt(i);
                i--;
            }

            if (!areItemsChanged) return;

            AddSomeOfEach(menu, itemsProcessed);

            RestackItems(menu, itemsProcessed);
        }

        private void DoQualitySmash(ItemGrabMenu menu)
        {

            var areItemsChanged = false;

            var inv = menu.ItemsToGrabMenu.actualInventory;

            var itemsProcessed = new List<Item>();

            modEntry.Monitor.Log(inv.Count.ToString(), LogLevel.Info);
            for (var i = 0; i < inv.Count; i++)
            {
                if (inv[i] == null || !(inv[i] is StardewValley.Object))
                {
                    modEntry.Monitor.Log("index: " + i + " is null or is not SDV Object", LogLevel.Info);
                    continue;
                }

                modEntry.Monitor.Log(inv[i].DisplayName + " " + (inv[i] as StardewValley.Object)?.Quality + " " + inv[i].Stack, LogLevel.Info);

                // Apply builtin and config filters

                if ((inv[i] as StardewValley.Object)?.Quality == 0) continue;

                if (config.IgnoreItemsQuality.Contains(inv[i].ParentSheetIndex) ||
                    config.IgnoreItemsCategory.Contains(inv[i].Category))
                    continue;
                
                if (!config.IgnoreIridiumItemExceptions.Contains(inv[i].ParentSheetIndex) &&
                    !config.IgnoreIridiumCategoryExceptions.Contains(inv[i].Category))
                    if (config.IgnoreIridium && (inv[i] as StardewValley.Object)?.Quality == 4) continue;

                if (config.IgnoreGold && (inv[i] as StardewValley.Object)?.Quality == 2) continue;

                if (config.IgnoreSilver && (inv[i] as StardewValley.Object)?.Quality == 1) continue;

                // Filtering complete, 

                areItemsChanged = true;


                if (inv[i] is StardewValley.Object o)
                    o.Quality = 0;

                itemsProcessed.Add(inv[i]);

                inv.RemoveAt(i);
                i--;
            }

            if (!areItemsChanged) return;

            AddSomeOfEach(menu, itemsProcessed);

            RestackItems(menu, itemsProcessed);
        }

        /// <summary>
        /// Modified version of the game's FillOutStacks method.
        /// </summary>
        /// <param name="menu">The active ItemGrabMenu (Chest, Fridge, etc.)</param>
        /// <param name="itemsToProcess">This list of items that were modified by the Smash methods</param>
        private void RestackItems(ItemGrabMenu menu, IList<Item> itemsToProcess)
        {
            var containerInventory = menu.ItemsToGrabMenu.actualInventory;

            for (var i = 0; i < containerInventory.Count; i++)
            {
                var containerItem = containerInventory[i];
                if (containerItem == null || containerItem.maximumStackSize() <= 1)
                    continue;

                for (var j = 0; j < itemsToProcess.Count; j++)
                {
                    var processingItem = itemsToProcess[j];
                    if (processingItem == null || !containerItem.canStackWith(processingItem))
                        continue;

                    var processingItemStackSize = processingItem.Stack;

                    if (containerItem.getRemainingStackSpace() > 0)
                    {
                        processingItemStackSize = containerItem.addToStack(processingItem);

                        menu?.ItemsToGrabMenu?.ShakeItem(containerItem);
                    }
                    processingItem.Stack = processingItemStackSize;

                    while (processingItem.Stack > 0)
                    {
                        Item overflowStack = null;

                        if (overflowStack == null)
                        {
                            for (var l = 0; l < containerInventory.Count; l++)
                            {
                                if (containerInventory[l] != null && containerInventory[l].canStackWith(containerItem) && containerInventory[l].getRemainingStackSpace() > 0)
                                {
                                    overflowStack = containerInventory[l];
                                    break;
                                }
                            }
                        }

                        if (overflowStack == null)
                        {
                            for (var k = 0; k < containerInventory.Count; k++)
                            {
                                if (containerInventory[k] == null)
                                {
                                    var item = containerInventory[k] = containerItem.getOne();
                                    overflowStack = item;
                                    overflowStack.Stack = 0;
                                    break;
                                }
                            }
                        }

                        if (overflowStack == null && containerInventory.Count < Chest.capacity)
                        {
                            overflowStack = containerItem.getOne();
                            overflowStack.Stack = 0;
                            containerInventory.Add(overflowStack);
                        }

                        if (overflowStack == null)
                        {
                            break;
                        }

                        processingItemStackSize = overflowStack.addToStack(processingItem);
                        menu.ItemsToGrabMenu.ShakeItem(containerItem);
                        processingItem.Stack = processingItemStackSize;
                    }

                    if (processingItem.Stack == 0)
                    {
                        itemsToProcess[j] = null;
                    }
                }
            }
        }

        /// <summary>
        /// This method is to "prime" the container with items so that RestackItems (FillOutStacks) will work
        /// </summary>
        /// <param name="menu">The active ItemGrabMenu (Chest, Fridge, etc.)</param>
        /// <param name="itemsToProcess">This list of items that were modified by the Smash methods</param>
        private void AddSomeOfEach(ItemGrabMenu menu, IList<Item> itemsToProcess)
        {
            var containerInventory = menu.ItemsToGrabMenu.actualInventory;

            // Handle edge case where container is empty after modifying every item in the container.
            // When the container is empty, the inner loop will never proceed, and no items will be re-added to the container
            if (containerInventory.Count == 0)
            {
                // Make the container not empty
                containerInventory.Add(itemsToProcess[0]);
                itemsToProcess.RemoveAt(0);
            }

            for (var i = 0; i < itemsToProcess.Count; i++)
            {
                if (itemsToProcess[i] == null || itemsToProcess[i].maximumStackSize() <= 1) 
                    continue;

                modEntry.Monitor.Log("Processing item: " + itemsToProcess[i].DisplayName + " " + (itemsToProcess[i] as Object)?.Quality, LogLevel.Info);

                for (var j = 0; j < containerInventory.Count; j++)
                {
                    // This is a nested 'if' because otherwise in an edge case where the last item in a chest
                    // is not stackable, no items will be added since the code does not continue on to "if (j + 1 == containerInventory.Count)"
                    if (containerInventory[j] != null && containerInventory[j].maximumStackSize() > 1)
                    {
                        // Found a stackable match, process the next item
                        if (containerInventory[j].canStackWith(itemsToProcess[i]))
                            break;
                    }

                    // Reached the end, and no stackable match was found, so add the item
                    if (j + 1 == containerInventory.Count)
                    {
                        containerInventory.Add(itemsToProcess[i]);
                        modEntry.Monitor.Log("Chest does not contain " + itemsToProcess[i].DisplayName + " " + (itemsToProcess[i] as Object)?.Quality, LogLevel.Info);
                        itemsToProcess[i] = null;
                        break;
                    }
                }
            }
        }
    }
}

