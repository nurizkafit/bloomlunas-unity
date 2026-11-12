using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI kota: papan rencana 7 hari, label dunia di atas lokasi, dan popup Go.
/// Semua data dibaca dari PlannerBook yang dipersist antar-scene.
/// </summary>
public class CityWorldUI : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private PlannerBook book;
    [SerializeField] private CityPlayerController player;

    private TextMeshProUGUI boardText;
    private GameObject popup;
    private TextMeshProUGUI popupTitle;
    private TextMeshProUGUI popupBody;
    private CityLocation selectedLocation;
    private readonly List<TextMeshPro> worldLabels = new List<TextMeshPro>();
    private readonly List<CityLocation> labelLocations = new List<CityLocation>();

    private void Start()
    {
        if (book == null) book = PlannerBook.Instance;
        BuildBoard();
        BuildPopup();
        BuildLocationLabels();
        if (book != null) book.PlannerChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (book != null) book.PlannerChanged -= Refresh;
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        for (int i = 0; i < worldLabels.Count && i < labelLocations.Count; i++)
        {
            TextMeshPro label = worldLabels[i];
            CityLocation location = labelLocations[i];
            if (label == null || location == null) continue;
            label.transform.position = location.LabelAnchor;
            label.transform.rotation = camera.transform.rotation;
        }
    }

    public void ShowLocation(CityLocation location)
    {
        selectedLocation = location;
        if (popup == null) return;

        popup.SetActive(true);
        if (popupTitle != null) popupTitle.text = location != null ? location.DisplayName : "Location";
        if (popupBody != null)
        {
            popupBody.text = location != null
                ? "Go will walk Player to this place."
                : "";
        }
    }

    private void Go()
    {
        if (selectedLocation == null) return;
        if (player == null) player = FindObjectOfType<CityPlayerController>(true);
        if (player != null) player.MoveTo(selectedLocation.FocusPoint);
        if (popup != null) popup.SetActive(false);
    }

    private void BuildBoard()
    {
        GameObject panel = CreatePanel("WeekPlanPanel", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -24f), new Vector2(360f, 225f));
        panel.transform.SetParent(transform, false);
        CreateText(panel.transform, "WEEK PLAN", 20f, new Color(0.18f, 0.14f, 0.10f, 1f),
            new Vector2(18f, -52f), new Vector2(-18f, 18f), TextAlignmentOptions.TopLeft, out boardText);
        boardText.rectTransform.anchorMin = new Vector2(0f, 0f);
        boardText.rectTransform.anchorMax = new Vector2(1f, 1f);
        boardText.rectTransform.offsetMin = new Vector2(18f, 14f);
        boardText.rectTransform.offsetMax = new Vector2(-18f, -52f);
        boardText.fontSize = 14f;
        boardText.lineSpacing = 4f;
        boardText.textWrappingMode = TextWrappingModes.Normal;
    }

    private void BuildPopup()
    {
        popup = CreatePanel("LocationPopup", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-24f, 24f), new Vector2(360f, 150f));
        popup.transform.SetParent(transform, false);

        CreateText(popup.transform, "Location", 22f, new Color(0.18f, 0.14f, 0.10f, 1f),
            new Vector2(18f, -44f), new Vector2(-18f, 18f), TextAlignmentOptions.TopLeft, out popupTitle);
        popupTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        popupTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        popupTitle.rectTransform.sizeDelta = new Vector2(-36f, 28f);

        CreateText(popup.transform, "", 14f, new Color(0.34f, 0.29f, 0.22f, 1f),
            new Vector2(18f, -82f), new Vector2(-18f, 18f), TextAlignmentOptions.TopLeft, out popupBody);
        popupBody.rectTransform.anchorMin = new Vector2(0f, 0f);
        popupBody.rectTransform.anchorMax = new Vector2(1f, 1f);
        popupBody.rectTransform.offsetMin = new Vector2(18f, 56f);
        popupBody.rectTransform.offsetMax = new Vector2(-18f, -82f);

        Button button = CreateButton(popup.transform, "Go", new Vector2(112f, 36f),
            new Vector2(-18f, 18f));
        button.onClick.AddListener(Go);
        popup.SetActive(false);
    }

    private void BuildLocationLabels()
    {
        CityLocation[] locations = FindObjectsOfType<CityLocation>(true);
        for (int i = 0; i < locations.Length; i++)
        {
            CityLocation location = locations[i];
            GameObject labelObject = new GameObject("Label_" + location.DisplayName,
                typeof(RectTransform), typeof(MeshRenderer), typeof(TextMeshPro));
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.text = location.DisplayName;
            label.fontSize = 3.2f;
            label.color = new Color(0.18f, 0.14f, 0.10f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(8f, 2f);
            if (font != null) label.font = font;
            else if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
            label.transform.position = location.LabelAnchor;
            worldLabels.Add(label);
            labelLocations.Add(location);
        }
    }

    private void Refresh()
    {
        if (boardText == null) return;
        if (book == null) book = PlannerBook.Instance;
        if (book == null)
        {
            boardText.text = "No plan loaded.";
            return;
        }

        int week = book.CurrentWeek != null ? book.CurrentWeek.week : 1;
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.AppendLine("WEEK " + week);
        for (int i = 0; i < 7; i++)
        {
            PlannerDayPlan day = book.GetDayPlan(i);
            string dayName = CoreLoop.DayNames[i].Substring(0, 3).ToUpperInvariant();
            if (day == null || !day.hasGoal)
            {
                text.AppendLine(dayName + "  -  no goal");
            }
            else
            {
                text.AppendLine(dayName + "  " + day.goal.activityName + "  ·  " + day.goal.subtext);
            }
        }
        boardText.text = text.ToString();
    }

    private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.sprite = panelSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.98f, 0.94f, 0.84f, 0.98f);
        image.raycastTarget = false;
        return panel;
    }

    private void CreateText(Transform parent, string text, float size, Color color,
        Vector2 position, Vector2 sizeDelta, TextAlignmentOptions alignment, out TextMeshProUGUI result)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;

        result = textObject.GetComponent<TextMeshProUGUI>();
        result.text = text;
        result.fontSize = size;
        result.color = color;
        result.alignment = alignment;
        result.raycastTarget = false;
        if (font != null) result.font = font;
        else if (TMP_Settings.defaultFontAsset != null) result.font = TMP_Settings.defaultFontAsset;
    }

    private Button CreateButton(Transform parent, string text, Vector2 size, Vector2 position)
    {
        GameObject buttonObject = new GameObject("GoButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 16f;
        label.color = new Color(0.18f, 0.14f, 0.10f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        if (font != null) label.font = font;
        else if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
        return button;
    }
}
