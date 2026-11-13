using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Kartu goal yang bisa ditarik dari dek ke salah satu hari.
/// Field-nya sengaja mengikuti Card_Prefab yang sudah ada.
/// </summary>
public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Prefab")]
    [SerializeField] private bool isDeckTemplate;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text iconText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image accentBar;
    [SerializeField] private Image iconBackground;
    [SerializeField] private Button deleteButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Data Kartu")]
    [SerializeField] private PlannerGoalCardData activity = new PlannerGoalCardData();
    [SerializeField] private Transform parentAfterDrag;

    [Header("Ikon Goal")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite exploreIcon;
    [SerializeField] private Sprite craftIcon;
    [SerializeField] private Sprite openShopIcon;
    [SerializeField] private Sprite marketGuildIcon;
    [SerializeField] private Sprite freeTimeIcon;
    [SerializeField] private Sprite customIcon;

    [Header("Preset")]
    [Tooltip("Kalau aktif, activity diisi ulang dari PlannerCatalog memakai presetGoal.")]
    [SerializeField] private bool applyPresetOnAwake = true;
    [SerializeField] private PlannerGoalType presetGoal = PlannerGoalType.Explore;

    [Header("Planner")]
    [SerializeField] private PlannerBookUI planner;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalOffsetMin;
    private Vector2 originalOffsetMax;
    private Vector2 originalSizeDelta;
    private Vector2 originalSize;
    private Vector2 originalPivot;
    private Vector3 originalLocalScale;
    private Vector2 dragOffset;
    private GameObject layoutPlaceholder;
    private bool slotPresentation;
    private Color deckTitleColor = Color.white;
    private Color deckSubColor = Color.white;
    private Color deckTimeColor = Color.white;
    private Vector2 deckIconSize = new Vector2(36f, 36f);

    public bool IsDeckTemplate => isDeckTemplate;
    public PlannerGoalCardData Activity => activity;
    public DropSlot CurrentSlot { get; set; }
    public PlannerBookUI Planner => planner;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (planner == null) planner = GetComponentInParent<PlannerBookUI>();

        // Kartu di dalam kotak hari adalah salinan; kartu di dek adalah template
        // yang tidak pernah pindah tempat.
        isDeckTemplate = GetComponentInParent<DropSlot>() == null;
        if (titleText != null) deckTitleColor = titleText.color;
        if (subText != null) deckSubColor = subText.color;
        if (timeText != null) deckTimeColor = timeText.color;
        if (iconImage != null) deckIconSize = iconImage.rectTransform.sizeDelta;

        if (applyPresetOnAwake)
        {
            activity = PlannerCatalog.GetGoal(presetGoal);
        }

        if (deleteButton != null) deleteButton.onClick.AddListener(DeleteFromSlot);
        SetDeckTemplate(isDeckTemplate);
        SetActivity(activity);
    }

    public void SetActivity(PlannerGoalCardData data)
    {
        if (data != null) activity = data;

        if (titleText != null) titleText.text = activity.activityName;
        if (subText != null) subText.text = activity.subtext;
        if (timeText != null) timeText.text = activity.category;
        if (iconText != null && !string.IsNullOrEmpty(activity.activityName))
        {
            iconText.text = activity.activityName.Substring(0, 1).ToUpperInvariant();
        }

        ApplyIcon();
        if (iconBackground != null) iconBackground.color = activity.accentColor;
        if (accentBar != null) accentBar.color = activity.accentColor;
        ApplyPresentation();
    }

    /// <summary>
    /// Kartu di slot dibuat ringkas: ikon + nama goal, dengan warna aksen.
    /// Kartu di dek tetap memakai tampilan kertas lengkap.
    /// </summary>
    public void SetSlotPresentation(bool compact)
    {
        slotPresentation = compact;
        ApplyPresentation();
    }

    private void ApplyPresentation()
    {
        if (cardBackground != null)
        {
            cardBackground.color = slotPresentation ? activity.accentColor : Color.white;
        }

        if (titleText != null)
        {
            titleText.color = slotPresentation ? Color.white : deckTitleColor;
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchoredPosition = slotPresentation
                ? new Vector2(48f, -10f)
                : new Vector2(54f, -5f);
            titleRect.sizeDelta = new Vector2(slotPresentation ? 210f : 190f, 34f);
        }

        if (subText != null)
        {
            subText.gameObject.SetActive(!slotPresentation);
            subText.color = deckSubColor;
        }

        if (timeText != null)
        {
            timeText.gameObject.SetActive(!slotPresentation);
            timeText.color = deckTimeColor;
        }

        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(!slotPresentation && !isDeckTemplate);
        }

        if (iconText != null && slotPresentation) iconText.color = Color.white;
        if (iconImage != null)
        {
            iconImage.rectTransform.sizeDelta = slotPresentation
                ? new Vector2(30f, 30f)
                : deckIconSize;
        }
    }

    private void ApplyIcon()
    {
        Sprite icon = IconFor(activity.goalType);
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
            iconImage.color = Color.white;
            iconImage.enabled = true;
            if (iconText != null) iconText.gameObject.SetActive(false);
            return;
        }

        if (iconText != null) iconText.gameObject.SetActive(true);
    }

    private Sprite IconFor(PlannerGoalType type)
    {
        switch (type)
        {
            case PlannerGoalType.Explore: return exploreIcon;
            case PlannerGoalType.Craft: return craftIcon;
            case PlannerGoalType.OpenShop: return openShopIcon;
            case PlannerGoalType.MarketGuild: return marketGuildIcon;
            case PlannerGoalType.FreeTime: return freeTimeIcon;
            default: return customIcon != null ? customIcon : exploreIcon;
        }
    }

    public void SetDeckTemplate(bool value)
    {
        isDeckTemplate = value;
        if (deleteButton != null) deleteButton.gameObject.SetActive(!value);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isDeckTemplate && planner != null && planner.CardPrefab == null)
        {
            // Tanpa prefab, kartu template tetap boleh dipakai sebagai kartu biasa.
        }

        parentAfterDrag = transform.parent;
        if (rectTransform != null)
        {
            originalAnchorMin = rectTransform.anchorMin;
            originalAnchorMax = rectTransform.anchorMax;
            originalOffsetMin = rectTransform.offsetMin;
            originalOffsetMax = rectTransform.offsetMax;
            originalSizeDelta = rectTransform.sizeDelta;
            originalSize = rectTransform.rect.size;
            originalLocalScale = rectTransform.localScale;
            originalPivot = rectTransform.pivot;
        }
        CreateLayoutPlaceholder();

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.85f;
        }

        if (rootCanvas != null && rectTransform != null)
        {
            Vector2 pointerLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position,
                eventData.pressEventCamera, out pointerLocal);
            Vector3 cardWorld = rectTransform.position;
            Vector2 cardLocal = canvasRect.InverseTransformPoint(cardWorld);
            dragOffset = cardLocal - pointerLocal;

            transform.SetParent(rootCanvas.transform, true);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = originalSize;
            rectTransform.localScale = originalLocalScale;
            rectTransform.position = cardWorld;
            transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null || rectTransform == null || canvasRect == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position,
                eventData.pressEventCamera, out localPoint))
        {
            rectTransform.localScale = originalLocalScale;
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ClearLayoutPlaceholder();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (CurrentSlot != null)
        {
            CurrentSlot.SetCard(this);
            return;
        }

        ReturnToParent();
    }

    public void ReturnToParent()
    {
        ClearLayoutPlaceholder();
        if (parentAfterDrag == null) return;

        transform.SetParent(parentAfterDrag, false);
        SetSlotPresentation(false);
        if (rectTransform != null)
        {
            rectTransform.anchorMin = originalAnchorMin;
            rectTransform.anchorMax = originalAnchorMax;
            rectTransform.pivot = originalPivot;
            rectTransform.offsetMin = originalOffsetMin;
            rectTransform.offsetMax = originalOffsetMax;
            rectTransform.sizeDelta = originalSizeDelta;
            rectTransform.localScale = originalLocalScale;
        }
    }

    private void CreateLayoutPlaceholder()
    {
        if (parentAfterDrag == null) return;
        if (parentAfterDrag.GetComponent<LayoutGroup>() == null) return;

        layoutPlaceholder = new GameObject("DragPlaceholder", typeof(RectTransform), typeof(LayoutElement));
        layoutPlaceholder.transform.SetParent(parentAfterDrag, false);
        layoutPlaceholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        RectTransform rect = layoutPlaceholder.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(originalSize.x, originalSize.y);

        LayoutElement element = layoutPlaceholder.GetComponent<LayoutElement>();
        element.minHeight = originalSize.y;
        element.preferredHeight = originalSize.y;
        element.preferredWidth = originalSize.x;
        element.flexibleWidth = 1f;
    }

    private void ClearLayoutPlaceholder()
    {
        if (layoutPlaceholder != null) Destroy(layoutPlaceholder);
        layoutPlaceholder = null;
    }

    public void DeleteFromSlot()
    {
        if (CurrentSlot != null && CurrentSlot.Planner != null)
        {
            CurrentSlot.Planner.ClearSlot(CurrentSlot);
            return;
        }

        gameObject.SetActive(false);
    }
}
