using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Общие фабрики UI-элементов (legacy UnityEngine.UI), используются всеми
// SceneBuilder*-скриптами. Шрифт — DejaVu Sans (Bitstream Vera, свободная
// лицензия) с поддержкой кириллицы. Встроенный Unity LegacyRuntime.ttf в WebGL
// рендерит только латиницу/цифры — кириллица пропадает.
public static class UiHelpers
{
    public const string ProjectFontPath =
        "Assets/_Project/Fonts/DejaVuSans.ttf";

    public static Font LoadProjectFont()
    {
        var f = AssetDatabase.LoadAssetAtPath<Font>(ProjectFontPath);
        if (f == null)
        {
            Debug.LogWarning("[UiHelpers] DejaVuSans.ttf не найден, " +
                "fallback на LegacyRuntime.ttf (кириллица будет пропадать)");
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return f;
    }

    public static Text Text(Transform parent, string name, Font font,
        int fontSize, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos,
        Vector2 size, TextAnchor alignment, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<UnityEngine.UI.Text>();
        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = alignment;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return t;
    }

    public static Button Button(Transform parent, string name, Font font,
        string label, Color color, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<UnityEngine.UI.Text>();
        text.text = label;
        text.font = font;
        text.fontSize = 44;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        // Тёмная обводка нужна на светлых пастелях (палитра кнопок главного
        // меню) — без неё белый текст плохо читается. На исторически тёмных
        // кнопках обводка не мешает.
        var outline = textGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(2, -2);

        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return btn;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
