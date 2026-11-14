using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menghubungkan UI scene lokasi (Bedroom, Home and Shop, Crafting Room, Guild)
/// dengan CoreLoop dan PlannerBook. Tidak membuat panel baru: semua tombol dan
/// panel sudah ada di scene, script ini hanya memasang aksinya.
/// </summary>
public class LocationUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CoreLoop coreLoop;
    [SerializeField] private PlannerBook book;

    [Header("Scene Tujuan")]
    [SerializeField] private string townScene = "CityOverWorld";
    [SerializeField] private string homeScene = "ShopdanRumah";
    [SerializeField] private string plannerScene = "MulaiGame";
    [SerializeField] private string craftingScene = "CraftingRoom";

    [Header("Panel")]
    [SerializeField] private GameObject restPanel;
    [SerializeField] private GameObject eatPanel;
    [SerializeField] private GameObject shortBreakPanel;
    [SerializeField] private GameObject sleepPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject productPanel;
    [SerializeField] private GameObject shelfPanel;

    [Header("Tombol Navigasi")]
    [SerializeField] private Button endDayButton;
    [SerializeField] private Button journalButton;
    [SerializeField] private Button townButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button craftingButton;
    [SerializeField] private Button seeTownButton;

    [Header("Tombol Panel")]
    [SerializeField] private Button restButton;
    [SerializeField] private Button eatButton;
    [SerializeField] private Button shortBreakButton;
    [SerializeField] private Button sleepButton;
    [SerializeField] private Button closeButton;
    [Tooltip("Teks umpan balik di panel (mis. hasil makan/istirahat).")]
    [SerializeField] private TMP_Text feedbackLabel;

    [Header("Home and Shop")]
    [SerializeField] private Button shopButton;
    [SerializeField] private Button openShopButton;

    [Header("Crafting")]
    [SerializeField] private Button grindingButton;
    [SerializeField] private Button dryingButton;
    [SerializeField] private Button craftingTableButton;

    [Header("Guild")]
    [SerializeField] private MarketStallUI stall;
    [SerializeField] private Button bulkLotsButton;
    [SerializeField] private Button lessonsButton;
    [SerializeField] private Button questBoardButton;

    [Header("Aturan Istirahat")]
    [SerializeField, Min(0)] private int mealEnergy = 2;
    [SerializeField, Min(0)] private int restEnergy = 2;
    [SerializeField, Min(0)] private int restMinutes = 120;
    [SerializeField, Min(0)] private int breakEnergy = 1;
    [SerializeField, Min(0)] private int breakMinutes = 60;
    [SerializeField, Min(0)] private int craftMinutes = 60;
    [SerializeField, Min(0)] private int bulkLotsQty = 10;

    private bool wired;

    private void Awake()
    {
        Resolve();
        Wire();
        HidePanels();
    }

    private void OnEnable()
    {
        Resolve();
        Wire();
    }

    private void Resolve()
    {
        if (coreLoop == null) coreLoop = CoreLoop.Instance;
        if (book == null) book = PlannerBook.Instance;
        if (stall == null) stall = GetComponent<MarketStallUI>();
        if (stall == null) stall = FindObjectOfType<MarketStallUI>(true);
    }

    private void Wire()
    {
        if (wired) return;
        wired = true;

        Wire(sleepButton, EndDay);
        Wire(seeTownButton, OpenTown);
        Wire(homeButton, OpenHome);
        Wire(craftingButton, OpenCrafting);

        // CityHudUI sudah menangani End day / Journal / Town; jangan dobel.
        if (GetComponent<CityHudUI>() == null)
        {
            Wire(endDayButton, EndDay);
            Wire(journalButton, OpenPlanner);
            Wire(townButton, OpenTown);
        }

        Wire(restButton, OpenRest);
        Wire(eatButton, EatMeal);
        Wire(shortBreakButton, TakeShortBreak);
        Wire(closeButton, HidePanels);

        Wire(shopButton, OpenShopShelf);
        Wire(openShopButton, OpenProductShelf);

        Wire(grindingButton, () => Craft(0));
        Wire(dryingButton, () => Craft(1));
        Wire(craftingTableButton, () => Craft(2));

        Wire(bulkLotsButton, BulkLots);
        Wire(lessonsButton, OpenPlanner);
        Wire(questBoardButton, OpenPlanner);
    }

    private void Wire(Button button, System.Action action)
    {
        if (button == null || action == null) return;
        button.onClick.AddListener(() => action());
    }

    // ---------------------------------------------------------------
    // Panel
    // ---------------------------------------------------------------

    private void HidePanels()
    {
        SetFeedback("");
        if (restPanel != null) restPanel.SetActive(false);
        if (eatPanel != null) eatPanel.SetActive(false);
        if (shortBreakPanel != null) shortBreakPanel.SetActive(false);
        if (sleepPanel != null) sleepPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (productPanel != null) productPanel.SetActive(false);
        if (shelfPanel != null) shelfPanel.SetActive(false);
    }

    private void OpenRest()
    {
        HidePanels();
        SetFeedback("");
        if (restPanel != null) restPanel.SetActive(true);
    }

    private void SetFeedback(string message)
    {
        if (feedbackLabel != null) feedbackLabel.text = message;
    }

    private string EnergyText()
    {
        if (coreLoop == null) return "";
        return "Energy " + coreLoop.Energy + "/" + coreLoop.MaxEnergy;
    }

    private void OpenShopShelf()
    {
        HidePanels();
        if (shopPanel != null) shopPanel.SetActive(true);
    }

    private void OpenProductShelf()
    {
        HidePanels();
        GameObject panel = productPanel != null ? productPanel : shelfPanel;
        if (panel != null) panel.SetActive(true);
    }

    // Aksi panel Bedroom tidak punya tombol konfirmasi sendiri di scene,
    // jadi tombol pembukanya langsung menjalankan aksinya.
    public void EatMeal()
    {
        if (book == null || coreLoop == null)
        {
            SetFeedback("Game data is not loaded.");
            return;
        }

        if (book.GetAmount("meal_pack") <= 0)
        {
            SetFeedback("No meal packs left. Buy food at the market first.");
            return;
        }

        book.AddInventory("meal_pack", "Meal pack", -1, false);
        coreLoop.RestoreEnergy(mealEnergy);
        SetFeedback("Ate 1 meal pack. " + EnergyText() +
                    " - " + book.GetAmount("meal_pack") + " pack(s) left.");
    }

    public void TakeShortBreak()
    {
        if (coreLoop == null)
        {
            SetFeedback("Game data is not loaded.");
            return;
        }

        coreLoop.SpendTime(breakMinutes, 0);
        coreLoop.RestoreEnergy(breakEnergy);
        SetFeedback("Short break: +" + breakEnergy + " energy. " + EnergyText() +
                    " - now " + coreLoop.ClockText + ".");
    }

    public void TakeRest()
    {
        if (coreLoop == null)
        {
            SetFeedback("Game data is not loaded.");
            return;
        }

        coreLoop.SpendTime(restMinutes, 0);
        coreLoop.RestoreEnergy(restEnergy);
        SetFeedback("Rested: +" + restEnergy + " energy. " + EnergyText() +
                    " - now " + coreLoop.ClockText + ".");
    }

    // ---------------------------------------------------------------
    // Navigasi
    // ---------------------------------------------------------------

    private void EndDay()
    {
        if (coreLoop == null) return;

        coreLoop.CompleteDay();

        if (coreLoop.Phase == GamePhase.DayReview)
        {
            coreLoop.ContinueAfterReview();
            ReloadScene();
            return;
        }

        if (coreLoop.Phase == GamePhase.WeekReview)
        {
            coreLoop.ContinueAfterWeek();
        }

        if (coreLoop.Phase == GamePhase.Planning || coreLoop.Phase == GamePhase.MonthEnd)
        {
            Load(plannerScene);
        }
    }

    private void OpenPlanner() => Load(plannerScene);
    private void OpenTown() => Load(townScene);
    private void OpenHome() => Load(homeScene);
    private void OpenCrafting() => Load(craftingScene);

    private void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (SceneManager.GetActiveScene().name == sceneName) return;
        SceneManager.LoadScene(sceneName);
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------------------------------------------------------------
    // Guild & Crafting
    // ---------------------------------------------------------------

    private void BulkLots()
    {
        if (stall != null) stall.FillBuy(bulkLotsQty);
    }

    private void Craft(int station)
    {
        if (book == null || coreLoop == null) return;

        string productId;
        string productName;
        PlannerSkillBranch branch = PlannerSkillBranch.Moon;

        if (station == 0)
        {
            productId = "turmeric_salep";
            productName = "Turmeric salep";
            if (!Consume("turmeric_fresh", 2) || !Consume("beeswax_block", 1)) return;
        }
        else if (station == 1)
        {
            productId = "lemongrass_balm";
            productName = "Lemongrass balm";
            if (!Consume("lemongrass_fresh", 2) || !Consume("coconut_oil", 1)) return;
        }
        else
        {
            productId = "beras_kencur";
            productName = "Beras kencur";
            if (!Consume("rice_grain", 2) || !Consume("kencur_fresh", 1)) return;
        }

        book.AddInventory(productId, productName, 1, true);
        book.AddSkillXp(branch, 3);
        book.AddLedger("Crafted 1 " + productName, coreLoop.DayIndex, 0, PlannerLedgerKind.Spend);
        coreLoop.SpendTime(craftMinutes, 1);
        HidePanels();
    }

    private bool Consume(string itemId, int amount)
    {
        if (book == null) return false;
        if (book.GetAmount(itemId) < amount) return false;
        book.AddInventory(itemId, itemId, -amount, false);
        return true;
    }
}
