using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Area kartu goal (dek) di halaman Goals. Menerima kartu yang ditarik dari
/// kotak hari supaya bisa dikembalikan ke dek, dan tidak membuat objek apa pun.
/// </summary>
public class DeckDropArea : MonoBehaviour, IDropHandler
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
        if (card == null) return;

        planner.ReturnCardToDeck(card);
    }
}
