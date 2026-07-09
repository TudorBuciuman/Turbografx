using UnityEngine;
using UnityEngine.UI;
using BITROOT.Inventory;

namespace BITROOT.UI
{
    /// <summary>Small floating panel: name + description + (weapon stats or consumable value). Follows the cursor.</summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text statsText;
        [SerializeField] private Vector2 cursorOffset = new Vector2(18f, -18f);

        private void Awake()
        {
            Hide();
        }

        private void Update()
        {
            if (panel != null && panel.gameObject.activeSelf)
            {
                panel.position = (Vector2)Input.mousePosition + cursorOffset;
            }
        }

        public void Show(ItemData item)
        {
            if (item == null || panel == null) return;

            panel.gameObject.SetActive(true);
            if (nameText != null) nameText.text = item.displayName;
            if (descriptionText != null) descriptionText.text = item.description;

            if (statsText != null)
            {
                statsText.text = BuildStatsString(item);
                statsText.gameObject.SetActive(!string.IsNullOrEmpty(statsText.text));
            }
        }

        public void Hide()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private string BuildStatsString(ItemData item)
        {
            switch (item)
            {
                case WeaponData weapon:
                    return weapon.weaponType switch
                    {
                        WeaponType.Gun => $"DMG {weapon.damage:0}   MAG {weapon.magazineSize}   RANGE {weapon.range:0}m",
                        WeaponType.Katana => $"DMG {weapon.damage:0}   COMBO x{weapon.comboLength}   FINISHER x{weapon.finisherMultiplier:0.0}",
                        WeaponType.Grenade => $"DMG {weapon.damage:0}   RADIUS {weapon.explosionRadius:0}m",
                        _ => string.Empty
                    };
                case ConsumableData consumable:
                    return consumable.effect switch
                    {
                        ConsumableEffect.FastHeal or ConsumableEffect.InstantHeal or ConsumableEffect.HealOverTime
                            => $"HEAL +{consumable.value:0}",
                        ConsumableEffect.AmmoRefill => $"AMMO +{consumable.value:0}",
                        _ => string.Empty
                    };
                default:
                    return string.Empty;
            }
        }
    }
}
