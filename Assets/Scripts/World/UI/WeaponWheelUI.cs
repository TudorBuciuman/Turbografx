using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BITROOT.Inventory;

namespace BITROOT.Combat
{
    /// <summary>
    /// Cyberpunk-style quick-select wheel: hold Left Alt to open, scroll/1-3 to
    /// highlight, release Alt to confirm. Doesn't touch gameplay logic directly -
    /// it only tells WeaponManager to switch once confirmed.
    ///
    /// Hierarchy expected (build once in the Canvas, wire references in the Inspector):
    ///   WeaponWheelRoot (this component, starts inactive)
    ///     - Background (full-screen dim panel, Image, alpha ~0.6)
    ///     - SlotContainer (empty RectTransform, children = one entry per weapon slot)
    ///         each entry: Image (icon) + TMP_Text (name) + Image (selection border)
    /// </summary>
    public class WeaponWheelUI : MonoBehaviour
    {
        [System.Serializable]
        public class WheelEntryUI
        {
            public GameObject root;
            public Image icon;
            public Text label;
            public Image selectionBorder;
        }

        [SerializeField] private GameObject wheelRoot;
        [SerializeField] private List<WheelEntryUI> entries = new List<WheelEntryUI>();
        [SerializeField] private WeaponManager weaponManager;

        [Header("Cyberpunk Styling")]
        [SerializeField] private Color unselectedColor = new Color(0.05f, 0.9f, 0.85f, 0.25f); // dim cyan
        [SerializeField] private Color selectedColor = new Color(1f, 0.05f, 0.55f, 1f);         // hot magenta

        public bool IsOpen { get; private set; }
        private int highlightedIndex;

        private void Awake()
        {
            if (wheelRoot != null) wheelRoot.SetActive(false);
        }

        public void Open()
        {
            if (IsOpen || weaponManager == null) return;
            IsOpen = true;
            highlightedIndex = 0;

            RefreshEntries();
            if (wheelRoot != null) wheelRoot.SetActive(true);

            Time.timeScale = 0.15f; // brief bullet-time while picking, remove if not wanted
            Cursor.lockState = CursorLockMode.None;
        }

        public void HighlightNext() => HighlightIndex((highlightedIndex + 1) % Mathf.Max(1, entries.Count));
        public void HighlightPrevious() => HighlightIndex((highlightedIndex - 1 + entries.Count) % Mathf.Max(1, entries.Count));

        public void HighlightIndex(int index)
        {
            if (index < 0 || index >= entries.Count) return;
            highlightedIndex = index;
            ApplyHighlightVisuals();
        }

        public void ConfirmAndClose()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Debug.Log("Over");
            if (highlightedIndex >= 0 && highlightedIndex < entries.Count)
            {
                weaponManager.SwitchTo(highlightedIndex);
            }

            if (wheelRoot != null) wheelRoot.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>Rebuilds icon/name per entry from the current weapon loadout each time it opens.</summary>
        private void RefreshEntries()
        {
            var owned = weaponManager.GetOwnedWeapons();
            for (int i = 0; i < entries.Count; i++)
            {
                bool hasWeapon = i < owned.Count;
                if (entries[i]?.root != null) entries[i].root.SetActive(hasWeapon);
                if (hasWeapon) BindEntry(i, owned[i]);
            }
            ApplyHighlightVisuals();
        }

        private void ApplyHighlightVisuals()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i]?.selectionBorder == null) continue;
                entries[i].selectionBorder.color = (i == highlightedIndex) ? selectedColor : unselectedColor;
            }
        }

        /// <summary>
        /// Call this once after populating `entries` in the Inspector (one entry per
        /// weapon the player owns) to push icon + label from WeaponData into the UI.
        /// </summary>
        public void BindEntry(int index, WeaponData data)
        {
            if (index < 0 || index >= entries.Count || data == null) return;
            if (entries[index].icon != null) entries[index].icon.sprite = data.icon;
            if (entries[index].label != null) entries[index].label.text = data.displayName;
        }
    }
}
