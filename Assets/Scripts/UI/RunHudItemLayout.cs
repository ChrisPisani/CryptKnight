using UnityEngine;

namespace CryptKnight.UI
{
    public static class RunHudItemLayout
    {
        public const int Columns = 8;
        public const float PanelWidth = 560f;
        public const float EmptyPanelHeight = 108f;

        private const float HorizontalPadding = 22f;
        private const float TopPadding = 20f;
        private const float HorizontalSpacing = 67f;
        private const float VerticalSpacing = 68f;
        private const float SlotSize = 62f;
        private const float BottomPadding = 26f;

        public static int GetRowCount(int itemCount)
        {
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0, itemCount) / (float)Columns));
        }

        public static float GetPanelHeight(int itemCount)
        {
            if (itemCount <= 0)
            {
                return EmptyPanelHeight;
            }

            return TopPadding + GetRowCount(itemCount) * SlotSize
                + (GetRowCount(itemCount) - 1) * (VerticalSpacing - SlotSize)
                + BottomPadding;
        }

        public static Vector2 GetItemPosition(int index)
        {
            int safeIndex = Mathf.Max(0, index);
            int row = safeIndex / Columns;
            int column = safeIndex % Columns;
            return new Vector2(
                HorizontalPadding + column * HorizontalSpacing,
                -TopPadding - row * VerticalSpacing);
        }
    }
}
