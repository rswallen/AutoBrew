using TMPro;
using UnityEngine;

namespace AutoBrew.UIElements
{
    internal static partial class UIUtilities
    {
        public static TMP_Dropdown SpawnDropdown()
        {
            GameObject obj = new()
            {
                name = $"{typeof(TMP_Dropdown).Name}",
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);
            var dropdown = obj.AddComponent<TMP_Dropdown>();
            dropdown.template = obj.AddComponent<RectTransform>();

            var lt = SpawnDescLocalizedText();
            dropdown.itemText = lt.text;
            dropdown.itemText.name = $"{typeof(TextMeshPro).Name}";
            dropdown.itemText.transform.SetParent(dropdown.transform, false);
            dropdown.itemText.transform.localPosition = new(0f, 0f);
            dropdown.itemText.gameObject.SetActive(true);
            Object.Destroy(lt, 0.001f);

            lt = SpawnDescLocalizedText();
            dropdown.captionText = lt.text;
            dropdown.captionText.name = $"{typeof(TextMeshPro).Name}";
            dropdown.captionText.transform.SetParent(dropdown.transform, false);
            dropdown.captionText.transform.localPosition = new(0f, 0f);
            dropdown.captionText.gameObject.SetActive(true);
            Object.Destroy(lt, 0.001f);

            obj.SetActive(true);
            return dropdown;
        }
    }
}
