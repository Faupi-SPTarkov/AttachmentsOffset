using EFT.InventoryLogic;
using System.Collections.Generic;
using UnityEngine;

namespace AttachmentsOffset
{
    public static class ItemViewCache
    {
        private static Dictionary<Item, List<Transform>> cachedItemViews = new Dictionary<Item, List<Transform>>();

        public static void Add(Item item, Transform itemView)
        {
            if (item == null || itemView == null)
                return;

            if (!cachedItemViews.ContainsKey(item))
                cachedItemViews.Add(item, new List<Transform>());

            if (!cachedItemViews[item].Contains(itemView))
                cachedItemViews[item].Add(itemView);
        }

        public static void Remove(Item item, Transform itemView)
        {
            if (item == null || itemView == null)
                return;

            if (!cachedItemViews.ContainsKey(item))
                return;

            if (cachedItemViews[item].Contains(itemView))
                cachedItemViews[item].Remove(itemView);

            if (cachedItemViews[item].Count == 0)
                cachedItemViews.Remove(item);
        }

        public static List<Transform> Get(Item item)
        {
            try
            {
                return cachedItemViews[item];
            }
            catch
            {
                return null;
            }
        }
    }
}
