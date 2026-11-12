using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Tempel di CardsContainer agar kartu bisa dikembalikan ke dek.</summary>
public class PlannerDeckDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private PlannerBookUI planner;

    private void Awake()
    {
        if (planner == null) planner = GetComponentInParent<PlannerBookUI>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (planner == null || eventData.pointerDrag == null) return;
        DraggableCard card = eventData.pointerDrag.GetComponent<DraggableCard>();
        if (card != null) planner.ReturnCardToDeck(card);
    }
}
