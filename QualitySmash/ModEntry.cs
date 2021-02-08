using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace QualitySmash
{
    public class ModEntry : Mod
    {
        private QualitySmashHandler handlerUiButtons;
        private SingleSmashHandler handlerKeybinds;
        private ModConfig config;

        internal IModHelper helper;

        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();

            var buttonColor = helper.Content.Load<Texture2D>("assets/buttonColor.png");
            var buttonQuality = helper.Content.Load<Texture2D>("assets/buttonQuality.png");

            this.helper = helper;
            this.handlerUiButtons = new QualitySmashHandler(this, config, buttonColor, buttonQuality);
            this.handlerKeybinds = new SingleSmashHandler(this, this.config);

            AddEvents(helper);

        }

        private void AddEvents(IModHelper helper)
        {
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.CursorMoved += OnCursorMoved;
        }

        /// <summary>
        /// Gets the ItemGrabMenu if it's from a fridge or chest
        /// </summary>
        /// <returns>The ItemGrabMenu</returns>
        internal MenuWithInventory GetContainerMenu()
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

        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var scaledMousePos = Game1.getMousePosition(true);

            handlerUiButtons.TryHover(scaledMousePos.X, scaledMousePos.Y);
        }

        /// <summary>
        /// Begins a check of whether a mouse click or button press was on a Smash button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (GetContainerMenu() != null)
                if (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA || e.Button == SButton.C)
                {
                    if (config.EnableUISmashButtons)
                        handlerUiButtons.HandleClick(e.Cursor);
                    if (config.EnableSingleItemSmashKeybinds)
                    {
                        if (helper.Input.IsDown(config.ColorSmashKeybind) ||
                            helper.Input.IsDown(config.QualitySmashKeybind))
                        {

                        }
                    }
                }
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (GetContainerMenu() != null)
                handlerUiButtons.DrawButtons();
        }
    }
}