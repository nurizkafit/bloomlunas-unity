using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Satu baris barang di Market stall (Buy atau Sell). Baris dibuat sekali oleh
/// tools editor, bukan saat Play.
/// </summary>
public class MarketRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text eachLabel;
    [SerializeField] private TMP_Text haveLabel;
    [SerializeField] private TMP_Text qtyLabel;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private string itemId = "beeswax_block";
    [SerializeField] private string itemName = "Beeswax block";
    [SerializeField] private bool isProduct;

    public string ItemId => itemId;
    public string ItemName => itemName;
    public bool IsProduct => isProduct;
    public TMP_Text NameLabel => nameLabel;
    public TMP_Text EachLabel => eachLabel;
    public TMP_Text HaveLabel => haveLabel;
    public TMP_Text QtyLabel => qtyLabel;
    public Button PlusButton => plusButton;
    public Button MinusButton => minusButton;

    public int Qty { get; private set; }

    public void Bind(string id, string displayName, bool product)
    {
        itemId = id;
        itemName = displayName;
        isProduct = product;
        if (nameLabel != null) nameLabel.text = displayName;
    }

    public void SetQty(int value)
    {
        Qty = Mathf.Max(0, value);
        if (qtyLabel != null) qtyLabel.text = Qty.ToString();
    }

    public void SetEach(string value)
    {
        if (eachLabel != null) eachLabel.text = value;
    }

    public void SetHave(string value)
    {
        if (haveLabel != null) haveLabel.text = value;
    }

    public void SetAvailable(bool value)
    {
        gameObject.SetActive(value);
    }
}
