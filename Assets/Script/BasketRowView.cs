using TMPro;
using UnityEngine;

/// <summary>
/// Satu baris isi keranjang di PanelInyourBasket. Baris tetap dibuat di scene
/// oleh tools editor, lalu hanya diaktifkan/diisi saat bermain.
/// </summary>
public class BasketRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private string itemId = "turmeric_fresh";
    [SerializeField] private string itemName = "Turmeric fresh";

    public string ItemId => itemId;
    public string ItemName => itemName;
    public TMP_Text NameLabel => nameLabel;
    public TMP_Text CountLabel => countLabel;

    public void Bind(string id, string displayName)
    {
        itemId = id;
        itemName = displayName;
        if (nameLabel != null) nameLabel.text = displayName;
    }

    public void Show(int amount)
    {
        gameObject.SetActive(amount > 0);
        if (countLabel != null) countLabel.text = "x" + Mathf.Max(0, amount);
    }
}
