using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Pembangun UI runtime yang dipakai semua panel meta-game (journal, craft,
/// shop, map, dialog NPC). Semua panel memakai kertas hangat dan tinta gelap
/// supaya konsisten dengan planner yang sudah ada.
/// </summary>
public static class UIKit
{
    public static readonly Color Paper = new Color(0.988f, 0.976f, 0.941f, 1f);
    public static readonly Color PaperDark = new Color(0.949f, 0.925f, 0.859f, 1f);
    public static readonly Color Ink = new Color(0.22f, 0.19f, 0.14f, 1f);
    public static readonly Color Muted = new Color(0.45f, 0.42f, 0.35f, 1f);
    public static readonly Color Gold = new Color(0.91f, 0.72f, 0.35f, 1f);
    public static readonly Color Green = new Color(0.38f, 0.55f, 0.34f, 1f);
    public static readonly Color Blue = new Color(0.33f, 0.45f, 0.55f, 1f);
    public static readonly Color Red = new Color(0.62f, 0.31f, 0.24f, 1f);
    public static readonly Color Line = new Color(0.78f, 0.72f, 0.59f, 1f);

    public static GameObject Panel(Transform parent, string name, Vector2 size, Color? color = null, bool border = true, int orderInLayer = 0)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        // Set anchors to stretch full-parent, pivot at center for sizeDelta-based positioning
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        // Ensure the object is on top of the UI hierarchy when freshly created
        obj.layer = parent?.gameObject?.layer ?? 0;

        Image image = obj.GetComponent<Image>();
        image.color = color ?? new Color(0.988f, 0.976f, 0.941f, 1f);
        image.raycastTarget = true;

        if (border)
        {
            // Only add outline if GraphicRaycaster is on parent canvas
            Canvas canvas = parent?.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.gameObject.GetComponent<GraphicRaycaster>() != null)
            {
                Outline outline = obj.AddComponent<Outline>();
                outline.effectColor = new Color(0.55f, 0.48f, 0.35f, 0.55f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        // Set order in layer for proper z-depth
        if (orderInLayer > 0) obj.transform.SetAsLastSibling();

        return obj;
    }

    public static GameObject Overlay(Transform parent, string name, float alpha = 0.55f)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        obj.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, alpha);
        return obj;
    }

    public static TextMeshProUGUI Text(Transform parent, string text, float size, Color? color = null,
        FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color ?? Ink;
        label.fontStyle = style;
        label.alignment = align;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    public static Button Button(Transform parent, string label, Vector2 size, Color color, UnityAction onClick,
        Color? textColor = null, float fontSize = 14f)
    {
        GameObject obj = new GameObject("Button_" + label, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        LayoutElement layout = obj.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.minWidth = size.x;
        layout.minHeight = size.y;

        obj.GetComponent<Image>().color = color;

        Button button = obj.GetComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(6f, 2f);
        textRt.offsetMax = new Vector2(-6f, -2f);

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor ?? new Color(0.2f, 0.15f, 0.05f, 1f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }

    public static GameObject Row(Transform parent, float height, float spacing = 8f)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = spacing;

        LayoutElement element = row.GetComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;

        return row;
    }

    public static VerticalLayoutGroup Vertical(Transform parent, float spacing = 6f, int padding = 0)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static RectTransform Scroll(Transform parent, float height, out RectTransform content)
    {
        GameObject scrollObj = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
        scrollObj.transform.SetParent(parent, false);

        LayoutElement element = scrollObj.GetComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
        element.flexibleHeight = height <= 0f ? 1f : 0f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewport.transform, false);

        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = contentObj.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 5f;
        contentLayout.padding = new RectOffset(4, 10, 4, 12);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObj.GetComponent<ScrollRect>();
        scroll.viewport = viewportRt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        content = contentRt;
        return scrollObj.GetComponent<RectTransform>();
    }

    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying) Object.Destroy(child);
            else Object.DestroyImmediate(child);
        }
    }

    public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    public static GameObject Divider(Transform parent)
    {
        GameObject line = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        line.transform.SetParent(parent, false);
        line.GetComponent<Image>().color = Line;
        LayoutElement element = line.GetComponent<LayoutElement>();
        element.preferredHeight = 1f;
        element.minHeight = 1f;
        return line;
    }

    public static GameObject Pill(Transform parent, string text, Color color)
    {
        GameObject pill = new GameObject("Pill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        pill.transform.SetParent(parent, false);
        pill.GetComponent<Image>().color = color;
        pill.GetComponent<LayoutElement>().preferredWidth = 92f;
        pill.GetComponent<LayoutElement>().preferredHeight = 24f;

        TextMeshProUGUI label = Text(pill.transform, text, 11f, new Color(0.99f, 0.96f, 0.88f, 1f),
            FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform rt = label.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return pill;
    }
}
