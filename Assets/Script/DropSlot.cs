using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Satu kotak hari di halaman Goals. Script ini mempertahankan nama class dan
/// field yang sudah tersimpan di scene MulaiGame.
/// </summary>
public class DropSlot : MonoBehaviour, IDropHandler, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hari")]
    [SerializeField] private int dayIndex;
    [SerializeField] private string timeSlot = "Day";

    [Header("Tampilan")]
    [SerializeField] private Image slotImage;
    [SerializeField] private TMP_Text emptyLabel;
    [SerializeField] private Outline selectionOutline;
    [SerializeField] private Color normalColor = new Color(0.973f, 0.949f, 0.886f, 1f);
    [SerializeField] private Color highlightColor = new Color(0.898f, 0.937f, 0.855f, 1f);
    [SerializeField] private Color swapColor = new Color(0.976f, 0.925f, 0.812f, 1f);
    [SerializeField] private Color blockedColor = new Color(0.945f, 0.851f, 0.831f, 1f);

    [field: SerializeField] public DraggableCard CurrentCard { get; set; }
    [field: SerializeField] public bool IsSelected { get; set; }

    [Header("Planner")]
    [SerializeField] private PlannerBookUI planner;
    [SerializeField] private string defaultEmptyLabel = "Drop a goal here";

    public int DayIndex => dayIndex;
    public string TimeSlot => timeSlot;
    public PlannerBookUI Planner => planner;

    private void Awake()
    {
        if (planner == null) planner = GetComponentInParent<PlannerBookUI>();
        RefreshVisual();
    }

    public void SetPlanner(PlannerBookUI value)
    {
        planner = value;
    }

    /// <summary>Dipakai setup otomatis jika indeks hari di scene kembar.</summary>
    public void SetDayIndex(int value)
    {
        dayIndex = value;
    }

    public void SetCard(DraggableCard card)
    {
        CurrentCard = card;
        if (CurrentCard != null)
        {
            CurrentCard.CurrentSlot = this;
            CurrentCard.SetSlotPresentation(true);
            CurrentCard.transform.SetParent(transform, false);
            CurrentCard.transform.SetAsLastSibling();
            RectTransform rect = CurrentCard.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
        }

        RefreshEmptyLabel();
    }

    /// <summary>
    /// Melepas kartu dari kotak hari. Kartu tidak dihancurkan: kartu slot sudah
    /// disiapkan di scene dan dipakai ulang, supaya tidak ada UI baru saat Play.
    /// </summary>
    public void ClearCard(bool destroyCard)
    {
        if (CurrentCard != null)
        {
            CurrentCard.CurrentSlot = null;
            CurrentCard.gameObject.SetActive(false);
        }

        CurrentCard = null;
        RefreshEmptyLabel();
    }

    public void RefreshFromPlanner()
    {
        PlannerBook book = planner != null ? planner.Book : PlannerBook.Instance;
        if (book == null)
        {
            RefreshVisual();
            return;
        }

        PlannerGoalCardData goal = book.GetDayGoal(dayIndex);

        if (goal == null)
        {
            if (CurrentCard != null) ClearCard(true);
            RefreshEmptyLabel();
            RefreshVisual();
            return;
        }

        if (CurrentCard != null)
        {
            CurrentCard.SetActivity(goal);
            CurrentCard.SetSlotPresentation(true);
            CurrentCard.CurrentSlot = this;
            CurrentCard.transform.SetParent(transform, false);
            CurrentCard.gameObject.SetActive(true);
            RefreshEmptyLabel();
            RefreshVisual();
            return;
        }

        if (planner != null)
        {
            DraggableCard created = planner.CreateSavedCard(this, goal);
            if (created != null)
            {
                SetCard(created);
                return;
            }
        }

        RefreshEmptyLabel();
        RefreshVisual();
    }

    public void Select(bool selected)
    {
        IsSelected = selected;
        RefreshVisual();
    }

    public void RefreshEmptyLabel()
    {
        if (emptyLabel == null) return;

        bool showEmpty = CurrentCard == null;
        emptyLabel.gameObject.SetActive(showEmpty);
        if (showEmpty)
        {
            PlannerBook book = planner != null ? planner.Book : PlannerBook.Instance;
            PlannerGoalCardData goal = book != null ? book.GetDayGoal(dayIndex) : null;
            emptyLabel.text = goal != null ? goal.activityName : defaultEmptyLabel;
        }
    }

    public void RefreshVisual()
    {
        if (slotImage != null)
        {
            slotImage.color = IsSelected ? highlightColor : normalColor;
        }

        if (selectionOutline != null)
        {
            selectionOutline.enabled = IsSelected;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableCard card = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableCard>()
            : null;

        if (card != null && planner != null)
        {
            planner.TryDropCard(this, card);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (planner != null) planner.SelectDay(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotImage != null && !IsSelected) slotImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RefreshVisual();
    }
}
