using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace QualitySmash
{
    public class ModEntry : Mod
    {
        internal enum SmashType
        {
            Color,
            Quality,
            Undo,
            None
        }

        internal static Dictionary<SmashType, string> TranslationMapping = new Dictionary<SmashType, string>()
        {
            { SmashType.Color, "hoverTextColor" },
            { SmashType.Quality, "hoverTextQuality" },
            { SmashType.Undo, "hoverTextUndo"}
        };

        private string assetsPath;

        private ButtonSmashHandler buttonSmashHandler;
        private SingleSmashHandler singleSmashHandler;
        private UndoHandler undoHandler;
        private ModConfig Config;

        // For GenericModConfigMenu
        private Dictionary<int, string> itemDictionary;
        private Dictionary<int, string> coloredItemDictionary;
        private Dictionary<int, string> categoryDictionary;

        internal IModHelper helper;

        public override void Entry(IModHelper helper)
        {
            this.assetsPath = Path.Combine(this.Helper.DirectoryPath, "assets");
            
            this.Config = helper.ReadConfig<ModConfig>();

            var buttonColor = helper.Content.Load<Texture2D>("assets/buttonColor.png");
            var buttonQuality = helper.Content.Load<Texture2D>("assets/buttonQuality.png");
            var buttonUndo = helper.Content.Load<Texture2D>("assets/buttonUndo.png");

            PopulateIdReferences();

            this.buttonSmashHandler = new ButtonSmashHandler(this, this.Config);

            if (Config.EnableUIColorSmashButton)
                this.buttonSmashHandler.AddButton(ModEntry.SmashType.Color, buttonColor, new Rectangle(0, 0, 16, 16));

            if (Config.EnableUIQualitySmashButton)
                this.buttonSmashHandler.AddButton(ModEntry.SmashType.Quality, buttonQuality, new Rectangle(0, 0, 16, 16));

            // Config for enable undo?

            this.singleSmashHandler = new SingleSmashHandler(this, this.Config, buttonColor, buttonQuality);

            this.helper = helper;

            AddEvents(helper);
        }

        // For use with generic mod config menu to display item/category names, but still be compatible with original config structure
        private void PopulateIdReferences()
        {
            itemDictionary = new Dictionary<int, string>();

            using (StreamReader fileStream = new StreamReader(Path.Combine(assetsPath, "ItemIDs.txt")))
            {
                string line = fileStream.ReadLine();

                while (line != null)
                {
                    string[] set = line.Split(',');
                    set[0] = set[0].Trim();
                    set[1] = set[1].Trim();

                    itemDictionary.Add(int.Parse(set[1]), set[0]);

                    line = fileStream.ReadLine();
                }
            }

            coloredItemDictionary = new Dictionary<int, string>();

            using (StreamReader fileStream = new StreamReader(Path.Combine(assetsPath, "ColoredItemIDs.txt")))
            {
                string line = fileStream.ReadLine();

                while (line != null)
                {
                    string[] set = line.Split(',');
                    set[0] = set[0].Trim();
                    set[1] = set[1].Trim();

                    coloredItemDictionary.Add(int.Parse(set[1]), set[0]);

                    line = fileStream.ReadLine();
                }
            }

            categoryDictionary = new Dictionary<int, string>();

            using (StreamReader fileStream = new StreamReader(Path.Combine(assetsPath, "CategoryIDs.txt")))
            {
                string line = fileStream.ReadLine();

                while (line != null)
                {
                    string[] set = line.Split(',');
                    set[0] = set[0].Trim();
                    set[1] = set[1].Trim();

                    categoryDictionary.Add(int.Parse(set[1]), set[0]);

                    line = fileStream.ReadLine();
                }
            }
        }

        private void AddEvents(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonReleased += OnButtonReleased;
            helper.Events.Input.CursorMoved += OnCursorMoved;
        }



        /// <summary>
        /// Gets the ItemGrabMenu if it's from a fridge or chest
        /// </summary>
        /// <returns>The ItemGrabMenu</returns>
        internal MenuWithInventory GetValidButtonSmashMenu()
        {
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

        internal IClickableMenu GetValidKeybindSmashMenu()
        {
            // InventoryMenu or MenuWithInventory.. Use ItemGrabMenu?
            if (Game1.activeClickableMenu != null &&
                (Game1.activeClickableMenu is ItemGrabMenu ||
                 Game1.activeClickableMenu is GameMenu)) 
                return Game1.activeClickableMenu;
            return null;
        }

        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            UpdateHoverText();
        }

        private void UpdateHoverText()
        {
            var scaledMousePos = Game1.getMousePosition(true);

            if (Config.EnableUISmashButtons)
                buttonSmashHandler.TryHover(scaledMousePos.X, scaledMousePos.Y);
            if (Config.EnableSingleItemSmashKeybinds)
                singleSmashHandler.TryHover(scaledMousePos.X, scaledMousePos.Y);
        }


        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu API (if it's installed)
            var api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api is null)
                return;

            // register mod configuration
            api.RegisterModConfig(
                mod: this.ModManifest,
                revertToDefault: () => this.Config = new ModConfig(),
                saveToFile: () => this.Helper.WriteConfig(this.Config)
            );

            api.SetDefaultIngameOptinValue(this.ModManifest, true);
            // add some Config options
            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Show UI Smash Buttons",
                optionDesc: "Show the color and quality smash buttons in the user interface",
                optionGet: () => this.Config.EnableUISmashButtons,
                optionSet: value => this.Config.EnableUISmashButtons = value
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Enable Color Smash UI Button",
                optionDesc: "Show the Color Smash button in the user interface. Requires \"Enable UI Buttons\" be enabled.",
                optionGet: () => this.Config.EnableUIColorSmashButton,
                optionSet: value =>
                {
                    this.Config.EnableUIColorSmashButton = value;
                    if (!value)
                        this.buttonSmashHandler.RemoveButton(ModEntry.SmashType.Color);
                    else
                        this.buttonSmashHandler.AddButton(ModEntry.SmashType.Color, helper.Content.Load<Texture2D>("assets/buttonColor.png"), new Rectangle(0, 0, 16, 16));
                });

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Enable Quality Smash UI Button",
                optionDesc: "Show the Quality Smash button in the user interface. Requires \"Enable UI Buttons\" be enabled.",
                optionGet: () => this.Config.EnableUIQualitySmashButton,
                optionSet: value =>
                {
                    this.Config.EnableUIQualitySmashButton = value;
                    if (!value)
                        this.buttonSmashHandler.RemoveButton(ModEntry.SmashType.Quality);
                    else
                        this.buttonSmashHandler.AddButton(ModEntry.SmashType.Quality, helper.Content.Load<Texture2D>("assets/buttonQuality.png"), new Rectangle(0, 0, 16, 16));
                });
            
            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Enable Egg Color Smash",
                optionDesc: "Enable egg colors to be smashed when Color Smashing with UI buttons or using keybinds",
                optionGet: () => this.Config.EnableEggColorSmashing,
                optionSet: value => this.Config.EnableEggColorSmashing = value
            );

            api.RegisterPageLabel(this.ModManifest, "Single Smash", "Configure an alternative method of color and quality smashing", "Single Smash");
            api.RegisterPageLabel(this.ModManifest, "Smash Filters", "Basic filters to exclude sets of items from Quality Smash", "Smash Filters");
            api.RegisterPageLabel(this.ModManifest, "Exceptions: Ignore Iridium", "Exceptions to the \"Ignore Iridium\" smash filter", "Exceptions: Ignore Iridium");
            api.RegisterPageLabel(this.ModManifest, "Exceptions: Ignore Iridium by Category", "Exceptions by category to the \"Ignore Iridium\" smash filter", "Exceptions: Ignore Iridium by Category");
            api.RegisterPageLabel(this.ModManifest, "Color Smash: Ignore Items", "Items to ignore when using the Color Smash button", "Color Smash: Ignore Items");
            api.RegisterPageLabel(this.ModManifest, "Both Smash: Ignore Items", "Items to ignore when using the Color Smash or Quality Smash buttons", "Both Smash: Ignore Items");
            api.RegisterPageLabel(this.ModManifest, "Both Smash: Ignore by Category", "Categories to ignore when using the Color Smash or Quality Smash buttons", "Both Smash: Ignore by Category");


            api.SetDefaultIngameOptinValue(this.ModManifest, true);
            api.StartNewPage(this.ModManifest, "Single Smash");
            api.RegisterPageLabel(this.ModManifest, "Back to main page", "", "");
            api.RegisterParagraph(this.ModManifest, "Single Smash is an alternative method of Color Smash and Quality Smash. It allows you to hold a keyboard key, then click an item in an inventory to smash color or quality.");
            api.RegisterParagraph(this.ModManifest, "When Color Smashing, the item will be smashed to \"default\" color. When Quality Smashing, the item will be reduced in quality by one step. Iridium -> Gold, Gold -> Silver, Silver -> Basic");

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Enable Single Smash Keybinds",
                optionDesc: "Enable smashing single items in any inventory by holding a designated key (configured below), then left clicking the item",
                optionGet: () => this.Config.EnableSingleItemSmashKeybinds,
                optionSet: value => this.Config.EnableSingleItemSmashKeybinds = value
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest, 
                optionName: "Color Smash Keybind", 
                optionDesc: "Button to hold when you wish to color smash a single item", 
                optionGet: () => this.Config.ColorSmashKeybind, 
                optionSet: (SButton val) => this.Config.ColorSmashKeybind = val
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Quality Smash Keybind",
                optionDesc: "Button to hold when you wish to quality smash a single item",
                optionGet: () => this.Config.QualitySmashKeybind,
                optionSet: (SButton val) => this.Config.QualitySmashKeybind = val
            );

            api.SetDefaultIngameOptinValue(this.ModManifest, true);
            api.StartNewPage(this.ModManifest, "Smash Filters");
            api.RegisterPageLabel(this.ModManifest, "Back to main page", "", "");
            api.RegisterParagraph(this.ModManifest, "Star qualities selected here will be ignored by Quality Smash UNLESS exceptions are specified in the config. See the Exceptions config pages");


            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Ignore Iridium Quality Items",
                optionDesc: "If enabled, iridium quality items will not be affected by \"Smash Quality\"",
                optionGet: () => this.Config.IgnoreIridium,
                optionSet: value => this.Config.IgnoreIridium = value
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Ignore Gold Quality Items",
                optionDesc: "If enabled, gold quality items will not be affected by \"Smash Quality\"",
                optionGet: () => this.Config.IgnoreGold,
                optionSet: value => this.Config.IgnoreGold = value
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Ignore Silver Quality Items",
                optionDesc: "If enabled, silver quality items will not be affected by \"Smash Quality\"",
                optionGet: () => this.Config.IgnoreSilver,
                optionSet: value => this.Config.IgnoreSilver = value
            );

            api.SetDefaultIngameOptinValue(this.ModManifest, true);
            api.StartNewPage(this.ModManifest, "Exceptions: Ignore Iridium");
            api.RegisterPageLabel(this.ModManifest, "Back to main page", "", "");
            api.RegisterParagraph(this.ModManifest, "Iridium quality items selected on this page WILL BE SMASHED by Smash Quality even if \"Ignore Iridium Quality Items\" is enabled");

            foreach (KeyValuePair<int, string> item in itemDictionary)
            {
                api.RegisterSimpleOption(
                    mod: this.ModManifest,
                    optionName: item.Value + " (" + item.Key + ")",
                    optionDesc: "Smash iridium quality " + item.Value + " even if \"Ignore Iridium Quality Items\" is enabled",
                    optionGet: () => Config.IgnoreIridiumItemExceptions.Contains(item.Key),
                    optionSet: value => ModConfig.SyncConfigSetting(value, item.Key, Config.IgnoreIridiumItemExceptions)
                );
            }

            api.SetDefaultIngameOptinValue(this.ModManifest, true);
            api.StartNewPage(this.ModManifest, "Exceptions: Ignore Iridium by Category");
            api.RegisterPageLabel(this.ModManifest, "Back to main page", "", "");
            api.RegisterParagraph(this.ModManifest, "Iridium quality items that fall under a category selected on this page WILL BE SMASHED by Smash Quality even if \"Ignore Iridium Quality Items\" is enabled");

            foreach (KeyValuePair<int, string> item in categoryDictionary)
            {
                api.RegisterSimpleOption(
                    mod: this.ModManifest,
                    optionName: item.Value + " (" + item.Key + ")",
                    optionDesc: "Smash iridium quality item within category " + item.Value + " even if \"Ignore Iridium Quality Items\" is enabled",
                    optionGet: () => Config.IgnoreIridiumCategoryExceptions.Contains(item.Key),
                    optionSet: value => ModConfig.SyncConfigSetting(value, item.Key, Config.IgnoreIridiumCategoryExceptions)
                );
            }

            api.SetDefaultIngameOptinValue(this.ModManifest, true);
            api.StartNewPage(this.ModManifest, "Color Smash: Ignore Items");
            api.RegisterPageLabel(this.ModManifest, "Back to main page", "", "");
            api.RegisterParagraph(this.ModManifest, "Items selected on this page will be ignored by Smash Colors");

            foreach (KeyValuePair<int, string> item in coloredItemDictionary)
            {
                api.RegisterSimpleOption(
                    mod: this.ModManifest,
                    optionName: item.Value + " (" + item.Key + ")",
                    optionDesc: item.Value + " will be ignored when pressing the Color Smash button",
                    optionGet: () => Config.IgnoreItemsColor.Contains(item.Key),
                    optionSet: value => ModConfig.SyncConfigSetting(value, item.Key, Config.IgnoreItemsColor)
                );
            }

            api.SetDefaultIngameOptinValue(this.ModManifest, true);
            api.StartNewPage(this.ModManifest, "Both Smash: Ignore Items");
            api.RegisterPageLabel(this.ModManifest, "Back to main page", "", "");
            api.RegisterParagraph(this.ModManifest, "Items selected on this page will be ignored by Smash Colors and Smash Quality");

            foreach (KeyValuePair<int, string> item in itemDictionary)
            {
                api.RegisterSimpleOption(
                    mod: this.ModManifest,
                    optionName: item.Value + " (" + item.Key + ")",
                    optionDesc: item.Value + " will be ignored when pressing the Quality Smash or Color Smash buttons",
                    optionGet: () => Config.IgnoreItemsQuality.Contains(item.Key),
                    optionSet: value => ModConfig.SyncConfigSetting(value, item.Key, Config.IgnoreItemsQuality)
                );
            }

            api.SetDefaultIngameOptinValue(this.ModManifest, true);
            api.StartNewPage(this.ModManifest, "Both Smash: Ignore by Category");
            api.RegisterPageLabel(this.ModManifest, "Back to main page", "", "");
            api.RegisterParagraph(this.ModManifest, "Items under categories selected on this page will be ignored by Smash Colors and Smash Quality");

            foreach (KeyValuePair<int, string> category in categoryDictionary)
            {
                api.RegisterSimpleOption(
                    mod: this.ModManifest,
                    optionName: category.Value + " (" + category.Key + ")",
                    optionDesc: "Items in category " + category.Value + " will be ignored when pressing the Quality Smash or Color Smash buttons",
                    optionGet: () => Config.IgnoreItemsCategory.Contains(category.Key),
                    optionSet: value => ModConfig.SyncConfigSetting(value, category.Key, Config.IgnoreItemsCategory)
                );
            }
        }

        //Attempt to smooth out button animations
        private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var menu = GetValidButtonSmashMenu();
            if (menu == null || !Config.EnableUISmashButtons)
                return;

            var scaledMousePos = Game1.getMousePosition(true);

            buttonSmashHandler.TryHover(scaledMousePos.X, scaledMousePos.Y);
        }

        /// <summary>
        /// Begins a check of whether a mouse click or button press was on a Smash button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (e.Button == Config.ColorSmashKeybind || e.Button == Config.QualitySmashKeybind)
            {
                UpdateHoverText();
                return;
            }

            if (e.Button != SButton.MouseLeft && e.Button != SButton.ControllerA)
                return;

            if (Config.EnableUISmashButtons && GetValidButtonSmashMenu() != null)
            {
                buttonSmashHandler.HandleClick(e);
            }

            if (Config.EnableSingleItemSmashKeybinds && GetValidKeybindSmashMenu() != null)
            {
                if (helper.Input.IsDown(Config.ColorSmashKeybind) ||
                    helper.Input.IsDown(Config.QualitySmashKeybind))
                {
                    singleSmashHandler.HandleClick(e);
                    helper.Input.Suppress(SButton.MouseLeft);
                    helper.Input.Suppress(SButton.ControllerA);
                }
            }
        }

        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button == Config.ColorSmashKeybind || e.Button == Config.QualitySmashKeybind)
            {
                UpdateHoverText();
                return;
            }
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (GetValidButtonSmashMenu() is ItemGrabMenu)
                if (Config.EnableUISmashButtons)
                    buttonSmashHandler.DrawButtons();

            if (GetValidKeybindSmashMenu() != null)
                if (Config.EnableSingleItemSmashKeybinds)
                    singleSmashHandler.DrawHoverText();
        }
    }
}