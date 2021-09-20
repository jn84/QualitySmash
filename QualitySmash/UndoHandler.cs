using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace QualitySmash
{
    internal class UndoHandler
    {
        private readonly Stack<IList<Item>> undoStack;

        internal UndoHandler()
        {
            undoStack = new Stack<IList<Item>>();
        }

        internal void PushUndo(IList<Item> inventoryToSave)
        {
            undoStack.Push(inventoryToSave);
        }

        internal IList<Item> PopUndo()
        {
            if (!CanUndo())
                return null;

            return undoStack.Pop();

        }

        internal bool CanUndo()
        {
            return undoStack.Any();
        }

        internal void Clear()
        {
            undoStack.Clear();
        }
    }
}
