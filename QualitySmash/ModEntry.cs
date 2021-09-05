using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace QualitySmash
{
    public class ModEntry : Mod
    {
        internal enum SmashType
        {
            Color,
            Quality,
            None
        }

        internal static Dictionary<SmashType, string> TranslationMapping = new Dictionary<SmashType, string>()
        {
            { SmashType.Color, "hoverTextColor" },
            { SmashType.Quality, "hoverTextQuality" },
        };

        private ButtonSmashHandler buttonSmashHandler;
        private SingleSmashHandler handlerKeybinds;
        private ModConfig Config;

        // For GenericModConfigMenu
        private Dictionary<int, string> itemNameDictionary;
        private Dictionary<string, int> itemIDDictionary;
        private Dictionary<int, string> categoryNameDictionary;
        private Dictionary<string, int> categoryIDDictionary;

        internal IModHelper helper;

        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();

            var buttonColor = helper.Content.Load<Texture2D>("assets/buttonColor.png");
            var buttonQuality = helper.Content.Load<Texture2D>("assets/buttonQuality.png");

            PopulateIDReferences();

            this.buttonSmashHandler = new ButtonSmashHandler(this, this.Config);

            if (Config.EnableUIColorSmashButton)
                this.buttonSmashHandler.AddButton(ModEntry.SmashType.Color, buttonColor, new Rectangle(0, 0, 16, 16));

            if (Config.EnableUIQualitySmashButton)
                this.buttonSmashHandler.AddButton(ModEntry.SmashType.Quality, buttonQuality, new Rectangle(0, 0, 16, 16));

            this.handlerKeybinds = new SingleSmashHandler(this, this.Config, buttonColor, buttonQuality);

            this.helper = helper;

            AddEvents(helper);
        }

        private void PopulateIDReferences()
        {
            itemNameDictionary = new Dictionary<int, string>();
            itemIDDictionary = new Dictionary<string, int>();

            // Populate Item ID dictionaries for use with GMCM

            using (StreamReader fileStream = new StreamReader("Mods/QualitySmash/assets/ItemIDs.txt"))
            {
                string line = fileStream.ReadLine();

                while (line != null)
                {
                    string[] set = line.Split(',');
                    set[0] = set[0].Trim();
                    set[1] = set[1].Trim();

                    itemNameDictionary.Add(int.Parse(set[1]), set[0]);
                    itemIDDictionary.Add(set[0], int.Parse(set[1]));

                    line = fileStream.ReadLine();
                }
            }

            categoryNameDictionary = new Dictionary<int, string>();
            categoryIDDictionary = new Dictionary<string, int>();

            // Populate Category ID dictionaries for use with GMCM

            using (StreamReader fileStream = new StreamReader("Mods/QualitySmash/assets/CategoryIDs.txt"))
            {
                string line = fileStream.ReadLine();

                while (line != null)
                {
                    string[] set = line.Split(',');
                    set[0] = set[0].Trim();
                    set[1] = set[1].Trim();

                    categoryNameDictionary.Add(int.Parse(set[0]), set[1]);
                    categoryIDDictionary.Add(set[1], int.Parse(set[0]));

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
                handlerKeybinds.TryHover(scaledMousePos.X, scaledMousePos.Y);
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

            // let players configure your mod in-game (instead of just from the title screen)
            api.SetDefaultIngameOptinValue(this.ModManifest, true);

            // add some Config options
            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "1234567890123456789012345678901234567890",
                optionDesc: "Show the color and quality smash buttons in the user interface",
                optionGet: () => this.Config.EnableUISmashButtons,
                optionSet: value => this.Config.EnableUISmashButtons = value
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Enable Color Smash UI Button",
                optionDesc: "Show the Color Smash button in the user interface. Requires \"Enable UI Buttons\" be enabled.",
                optionGet: () => this.Config.EnableUIColorSmashButton,
                optionSet: value => this.Config.EnableUIColorSmashButton = value
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Enable Quality Smash UI Button",
                optionDesc: "Show the Quality Smash button in the user interface. Requires \"Enable UI Buttons\" be enabled.",
                optionGet: () => this.Config.EnableUIQualitySmashButton,
                optionSet: value => this.Config.EnableUIQualitySmashButton = value
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Enable egg Color Smash",
                optionDesc: "Enable egg colors to be smashed when Color Smashing with UI buttons or using keybinds",
                optionGet: () => this.Config.EnableEggColorSmashing,
                optionSet: value => this.Config.EnableEggColorSmashing = value
            );

            // Single Smash Configs

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Enable Single Smash Keybinds",
                optionDesc: "Enable smashing single items in any inventory by holding a designated key (configured below), then left clicking the item",
                optionGet: () => this.Config.EnableSingleItemSmashKeybinds,
                optionSet: value => this.Config.EnableSingleItemSmashKeybinds = value
            );

            // Code to disable keybind config when above is disabled?

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

            // Filters

            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Do Not Smash Iridium Quality Items",
                optionDesc: "If enabled, iridium quality items will not be affected by \"Smash Quality\"",
                optionGet: () => this.Config.IgnoreIridium,
                optionSet: value => this.Config.IgnoreIridium = value
            );
            
            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Do Not Smash Gold Quality Items",
                optionDesc: "If enabled, gold quality items will not be affected by \"Smash Quality\"",
                optionGet: () => this.Config.IgnoreGold,
                optionSet: value => this.Config.IgnoreGold = value
            );
            
            api.RegisterSimpleOption(
                mod: this.ModManifest,
                optionName: "Do Not Smash Silver Quality Items",
                optionDesc: "If enabled, silver quality items will not be affected by \"Smash Quality\"",
                optionGet: () => this.Config.IgnoreSilver,
                optionSet: value => this.Config.IgnoreSilver = value
            );

            api.RegisterSimpleOption(
                mod: this.ModManifest, 
                optionName: "Ignore Items", 
                optionDesc: "Items listed here will not be affected when pressing the Quality Smash button. Comma separated. Can be either item IDs or item name (as displayed in game)", 
                optionGet:() => Helpers.GetNameString(Config.IgnoreItemsQuality, itemNameDictionary), // Parse the config and create comma delimited string of item names
                optionSet: (string val) => Config.IgnoreItemsQuality = Helpers.GetIdList(val, itemIDDictionary)
            ); // Convert the text box text (strings or ints) into a List<int> of item IDs

            api.StartNewPage(this.ModManifest, "Test Page");




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
                    handlerKeybinds.HandleClick(e);
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
                    handlerKeybinds.DrawHoverText();
        }
    }
}