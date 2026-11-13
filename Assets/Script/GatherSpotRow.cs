using TMPro;
using UnityEngine;

/// <summary>
/// Satu baris daftar "Herbs out here" di PanelCityOutskirt.
/// Data baris diisi lewat Inspector oleh tools editor; tidak dibuat saat Play.
/// </summary>
public class GatherSpotRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text timeLabel;
    [SerializeField] private string itemId = "turmeric_fresh";
    [SerializeField] private string itemName = "Turmeric fresh";
    [SerializeField, Min(1)] private int amount = 2;
    [SerializeField, Min(1)] private int minutes = 60;

    public TMP_Text NameLabel => nameLabel;
    public TMP_Text TimeLabel => timeLabel;
    public string ItemId => itemId;
    public string ItemName => itemName;
    public int Amount => Mathf.Max(1, amount);
    public int Minutes => Mathf.Max(1, minutes);

    public string TimeText => minutes >= 60
        ? (minutes / 60f).ToString("0.#") + "h"
        : minutes + "m";

    public void Fill(string id, string displayName, int unitAmount, int durationMinutes)
    {
        itemId = id;
        itemName = displayName;
        amount = Mathf.Max(1, unitAmount);
        minutes = Mathf.Max(1, durationMinutes);
        if (nameLabel != null) nameLabel.text = displayName;
        if (timeLabel != null) timeLabel.text = TimeText;
    }
}
