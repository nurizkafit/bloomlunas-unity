using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Market stall di scene Market: tab Buy, Food, dan Sell. Semua baris barang
/// sudah ada di scene; script hanya mengisi angka dan menjalankan transaksi
/// lewat PlannerBook/CoreLoop supaya uang, bahan, dan produk tersimpan.
/// </summary>
public class MarketStallUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CoreLoop coreLoop;
    [SerializeField] private PlannerBook book;

    [Header("Buka / Tutup")]
    [SerializeField] private GameObject stallPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Tab")]
    [Tooltip("Guild menampilkan semua panel sekaligus, jadi tab tidak menyembunyikan panel.")]
    [SerializeField] private bool singlePanelTabs = true;
    [SerializeField] private Button tabBuy;
    [SerializeField] private Button tabFood;
    [SerializeField] private Button tabSell;
    [SerializeField] private GameObject buyPanel;
    [SerializeField] private GameObject foodPanel;
    [SerializeField] private GameObject sellPanel;

    [Header("Buy")]
    [SerializeField] private MarketRowView[] buyRows;
    [SerializeField] private TMP_Text buyTotal;
    [SerializeField] private TMP_Text storeLeft;
    [SerializeField] private Button buyButton;

    [Header("Food")]
    [SerializeField] private Button foodPlus;
    [SerializeField] private Button foodMinus;
    [SerializeField] private TMP_Text foodQty;
    [SerializeField] private TMP_Text foodTotal;
    [SerializeField] private TMP_Text foodDays;
    [SerializeField] private TMP_Text foodPrice;
    [SerializeField] private TMP_Text personalLeft;
    [SerializeField] private TMP_Text mealpackHave;
    [SerializeField] private Button foodBuyButton;

    [Header("Sell")]
    [SerializeField] private MarketRowView[] sellRows;
    [SerializeField] private TMP_Text sellTotal;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button selectAllButton;
    [SerializeField] private Button deselectAllButton;

    [Header("Aturan")]
    [SerializeField, Min(0)] private int mealPackPrice = 3;
    [SerializeField, Min(1)] private int mealPacksPerDay = 3;
    [SerializeField, Min(0)] private int tradeMinutes = 15;

    private int foodQtyValue;
    private bool wired;

    private void Awake()
    {
        Resolve();
        Wire();
        if (stallPanel != null) stallPanel.SetActive(false);
    }

    private void OnEnable()
    {
        Resolve();
        Wire();
        if (book != null) book.PlannerChanged += Refresh;
        if (coreLoop != null) coreLoop.StateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (book != null) book.PlannerChanged -= Refresh;
        if (coreLoop != null) coreLoop.StateChanged -= Refresh;
    }

    private void Resolve()
    {
        if (coreLoop == null) coreLoop = CoreLoop.Instance;
        if (book == null) book = PlannerBook.Instance;
    }

    private void Wire()
    {
        if (wired) return;
        wired = true;

        Wire(openButton, Open);
        Wire(closeButton, Close);
        Wire(tabBuy, () => ShowTab(0));
        Wire(tabFood, () => ShowTab(1));
        Wire(tabSell, () => ShowTab(2));
        Wire(buyButton, BuyBasket);
        Wire(foodPlus, () => ChangeFood(1));
        Wire(foodMinus, () => ChangeFood(-1));
        Wire(foodBuyButton, BuyFood);
        Wire(sellButton, SellBasket);
        Wire(selectAllButton, SelectAllSell);
        Wire(deselectAllButton, ClearSell);

        if (buyRows != null)
        {
            for (int i = 0; i < buyRows.Length; i++)
            {
                MarketRowView row = buyRows[i];
                if (row == null) continue;
                Wire(row.PlusButton, () => ChangeBuy(row, 1));
                Wire(row.MinusButton, () => ChangeBuy(row, -1));
            }
        }

        if (sellRows != null)
        {
            for (int i = 0; i < sellRows.Length; i++)
            {
                MarketRowView row = sellRows[i];
                if (row == null) continue;
                Wire(row.PlusButton, () => ChangeSell(row, 1));
                Wire(row.MinusButton, () => ChangeSell(row, -1));
            }
        }
    }

    private void Wire(Button button, System.Action action)
    {
        if (button == null || action == null) return;
        button.onClick.AddListener(() => action());
    }

    public void Open()
    {
        Resolve();
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (stallPanel != null) stallPanel.SetActive(true);
        ShowTab(0);
        Refresh();
    }

    public void Close()
    {
        if (stallPanel != null) stallPanel.SetActive(false);
    }

    private void ShowTab(int index)
    {
        if (singlePanelTabs)
        {
            if (buyPanel != null) buyPanel.SetActive(index == 0);
            if (foodPanel != null) foodPanel.SetActive(index == 1);
            if (sellPanel != null) sellPanel.SetActive(index == 2);
        }
        Refresh();
    }

    /// <summary>Guild "Bulk lots": isi semua baris beli dengan jumlah yang sama.</summary>
    public void FillBuy(int qty)
    {
        if (buyRows == null) return;
        for (int i = 0; i < buyRows.Length; i++)
        {
            if (buyRows[i] != null) buyRows[i].SetQty(Mathf.Max(0, qty));
        }
        RefreshBuy();
    }

    // ---------------------------------------------------------------
    // Buy
    // ---------------------------------------------------------------

    private void ChangeBuy(MarketRowView row, int delta)
    {
        if (row == null) return;
        row.SetQty(row.Qty + delta);
        RefreshBuy();
    }

    private void BuyBasket()
    {
        if (book == null || buyRows == null) return;

        int total = BuyTotal();
        if (total <= 0) return;
        if (!book.SpendFromJar(PlannerJarKind.Store, total)) return;

        for (int i = 0; i < buyRows.Length; i++)
        {
            MarketRowView row = buyRows[i];
            if (row == null || row.Qty <= 0) continue;
            book.AddInventory(row.ItemId, row.ItemName, row.Qty, false);
            row.SetQty(0);
        }

        SpendTime();
        Refresh();
    }

    private int BuyTotal()
    {
        if (book == null || buyRows == null) return 0;

        float total = 0f;
        for (int i = 0; i < buyRows.Length; i++)
        {
            MarketRowView row = buyRows[i];
            if (row == null || row.Qty <= 0) continue;
            total += book.GetPrice(row.ItemId) * row.Qty;
        }
        return Mathf.RoundToInt(total);
    }

    // ---------------------------------------------------------------
    // Food
    // ---------------------------------------------------------------

    private void ChangeFood(int delta)
    {
        foodQtyValue = Mathf.Max(0, foodQtyValue + delta);
        RefreshFood();
    }

    private void BuyFood()
    {
        if (book == null || foodQtyValue <= 0) return;

        int total = foodQtyValue * mealPackPrice;
        if (!book.SpendFromJar(PlannerJarKind.Personal, total)) return;

        book.AddInventory("meal_pack", "Meal pack", foodQtyValue, false);
        foodQtyValue = 0;
        SpendTime();
        Refresh();
    }

    // ---------------------------------------------------------------
    // Sell
    // ---------------------------------------------------------------

    private void ChangeSell(MarketRowView row, int delta)
    {
        if (row == null || book == null) return;
        int spare = book.GetAmount(row.ItemId);
        row.SetQty(Mathf.Clamp(row.Qty + delta, 0, spare));
        RefreshSell();
    }

    private void SelectAllSell()
    {
        if (sellRows == null || book == null) return;
        for (int i = 0; i < sellRows.Length; i++)
        {
            MarketRowView row = sellRows[i];
            if (row == null) continue;
            row.SetQty(book.GetAmount(row.ItemId));
        }
        RefreshSell();
    }

    private void ClearSell()
    {
        if (sellRows == null) return;
        for (int i = 0; i < sellRows.Length; i++)
        {
            if (sellRows[i] != null) sellRows[i].SetQty(0);
        }
        RefreshSell();
    }

    private void SellBasket()
    {
        if (book == null || sellRows == null) return;

        bool sold = false;
        for (int i = 0; i < sellRows.Length; i++)
        {
            MarketRowView row = sellRows[i];
            if (row == null || row.Qty <= 0) continue;
            if (book.SellProduct(row.ItemId, row.Qty))
            {
                sold = true;
                row.SetQty(0);
            }
        }

        if (sold) SpendTime();
        Refresh();
    }

    private int SellTotal()
    {
        if (book == null || sellRows == null) return 0;

        float total = 0f;
        for (int i = 0; i < sellRows.Length; i++)
        {
            MarketRowView row = sellRows[i];
            if (row == null || row.Qty <= 0) continue;
            total += book.GetSellPrice(row.ItemId) * row.Qty;
        }
        return Mathf.RoundToInt(total);
    }

    // ---------------------------------------------------------------
    // Refresh
    // ---------------------------------------------------------------

    private void Refresh()
    {
        RefreshBuy();
        RefreshFood();
        RefreshSell();
    }

    private void RefreshBuy()
    {
        if (book == null) return;

        if (buyRows != null)
        {
            for (int i = 0; i < buyRows.Length; i++)
            {
                MarketRowView row = buyRows[i];
                if (row == null) continue;
                row.SetEach(book.GetPrice(row.ItemId).ToString("0.#"));
                row.SetHave(book.GetAmount(row.ItemId).ToString());
            }
        }

        int total = BuyTotal();
        if (buyTotal != null) buyTotal.text = "-" + total;
        if (storeLeft != null) storeLeft.text = "Store jar left " + (book.Budget.store - total);
        if (buyButton != null) buyButton.interactable = total > 0 && total <= book.Budget.store;
    }

    private void RefreshFood()
    {
        if (book == null) return;

        int total = foodQtyValue * mealPackPrice;
        if (foodQty != null) foodQty.text = foodQtyValue.ToString();
        if (foodTotal != null) foodTotal.text = "-" + total;
        if (foodPrice != null) foodPrice.text = mealPackPrice.ToString();
        if (foodDays != null) foodDays.text = (foodQtyValue / Mathf.Max(1, mealPacksPerDay)).ToString();
        if (mealpackHave != null) mealpackHave.text = book.GetAmount("meal_pack").ToString();
        if (personalLeft != null)
        {
            personalLeft.text = "Personal jar left " + (book.Budget.personal - total);
        }
        if (foodBuyButton != null)
        {
            foodBuyButton.interactable = total > 0 && total <= book.Budget.personal;
        }
    }

    private void RefreshSell()
    {
        if (book == null) return;

        if (sellRows != null)
        {
            for (int i = 0; i < sellRows.Length; i++)
            {
                MarketRowView row = sellRows[i];
                if (row == null) continue;
                int spare = book.GetAmount(row.ItemId);
                row.SetAvailable(spare > 0 || row.Qty > 0);
                row.SetQty(Mathf.Clamp(row.Qty, 0, spare));
                row.SetEach(book.GetSellPrice(row.ItemId).ToString("0.#"));
                row.SetHave(spare.ToString());
            }
        }

        int total = SellTotal();
        if (sellTotal != null) sellTotal.text = "+" + total;
        if (sellButton != null) sellButton.interactable = total > 0;
    }

    private void SpendTime()
    {
        if (coreLoop != null) coreLoop.SpendTime(tradeMinutes, 0);
    }
}
