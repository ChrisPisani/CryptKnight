using CryptKnight.Loot;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CryptKnight.UI
{
    public sealed class RunPauseMenuItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RunPauseMenuController owner;
        private LootItemDefinition itemDefinition;
        private int quantity;

        public void Initialize(RunPauseMenuController pauseMenu, LootItemDefinition definition, int itemQuantity)
        {
            owner = pauseMenu;
            itemDefinition = definition;
            quantity = itemQuantity;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.ShowItemTooltip(itemDefinition, quantity);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HideItemTooltip();
        }
    }
}
