using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;


namespace QualitySmash
{
    public class ModEntry : Mod
    {
        private QualitySmashHandler handler;

        internal IModHelper helper;

        private ModConfig config;

        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();

            var buttonColor = helper.Content.Load<Texture2D>("assets/buttonColor.png");
            var buttonQuality = helper.Content.Load<Texture2D>("assets/buttonQuality.png");

            this.helper = helper;
            this.handler = new QualitySmashHandler(this, config, buttonColor, buttonQuality);

            AddEvents(helper);

        }

        private void AddEvents(IModHelper helper)
        {
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.CursorMoved += OnCursorMoved;
        }

        /// <summary>
        /// Gets the ItemGrabMenu
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

            var scaledMousePos = new Point(Game1.getMouseX(true), Game1.getMouseY(true));
            
            handler.TryHover(scaledMousePos.X, scaledMousePos.Y);
        }

        /// <summary>
        /// Begins a check if a mouse click or button press was on a Smash button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var menu = GetContainerMenu();
            
            if (menu != null)
                if (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA || e.Button == SButton.C)
                    handler.HandleClick(e.Cursor);
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (GetContainerMenu() != null)
                handler.DrawButtons();
        }
    }
}