using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace QualitySmash
{
    internal class QSButton
    {
        public ModEntry.SmashType smashType { get; private set; }

        private Texture2D texture;

        private ClickableTextureComponent clickable;

        private Rectangle bounds; // hmmmm

        public QSButton(ModEntry.SmashType smashType, Texture2D texture, Rectangle buttonClickableArea)
        {
            this.smashType = smashType;

            this.texture = texture;

            clickable = new ClickableTextureComponent(Rectangle.Empty, texture, buttonClickableArea, 4f);
        }



        public void SetBounds(int screenX, int screenY, int size)
        {
            clickable.bounds = new Rectangle(screenX, screenY, size, size);
        }

        public void DrawButton(SpriteBatch b)
        {
            if (this.bounds == null)
                throw new Exception("QSButton: SetBounds not called. Cannot draw button");
            clickable.draw(b, Color.White, 0f, 0);
            if (clickable.hoverText != "")
                IClickableMenu.drawHoverText(b, clickable.hoverText, Game1.smallFont);
        }

        //Ensure passing scaled pixels
        public bool ContainsPoint(int x, int y)
        {
            return clickable.containsPoint(x, y);
        }

        public void UpdateHoverText(string hoverText)
        {
            clickable.hoverText = hoverText;
        }

        /// <summary>
        /// Scale the button if hovered
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void TryHover(int x, int y)
        {
            clickable.tryHover(x, y, 0.4f);
        }
        
    }
}
