using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Controller untuk objek PlannerBookUI yang sudah ada di scene MulaiGame.
/// Semua referensi UI diisi lewat Inspector; script tidak membuat panel baru.
/// </summary>
[DefaultExecutionOrder(-80)]
public class PlannerBookUI : PlannerBookManager
{
    [Header("Data")]
    [SerializeField] private PlannerBook book;
    [SerializeField] private CoreLoop coreLoop;

    [Header("Header")]
    [SerializeField] private TMP_Text weekLabel;
    [SerializeField] private TMP_Text goldLabel;
    [SerializeField] private TMP_Text debtLabel;
    [SerializeField] private TMP_Text nextPaymentLabel;
    [SerializeField] private TMP_Text goldAfterLabel;
    [SerializeField] private TMP_Text energyLabel;
    [SerializeField] private GameObject warningChip;
    [SerializeField] private TMP_Text warningText;

    [Header("Goals")]
    [SerializeField] private DropSlot[] slots;
    [SerializeField] private DraggableCard cardPrefab;
    [SerializeField] private Transform deckContainer;
    [SerializeField] private TMP_Text goalCountLabel;
    [Tooltip("Buat kartu dari prefab untuk rencana yang sudah tersimpan saat halaman dibuka.")]
    [SerializeField] private bool buildSavedCards = true;

    [Header("Review Plan")]
    [SerializeField] private GameObject reviewModal;
    [SerializeField] private TMP_Text reviewSummaryLabel;
    [SerializeField] private TMP_Text reviewWarningLabel;
    [SerializeField] private TMP_Text reviewSalesGoalLabel;
    [SerializeField] private TMP_Text reviewHerbsLabel;
    [SerializeField] private TMP_Text reviewLivingLabel;
    [SerializeField] private TMP_Text reviewSalesGuessLabel;
    [SerializeField] private TMP_Text reviewGoldAfterLabel;
    [SerializeField] private Button startAnywayButton;
    [SerializeField] private Button keepPlanningButton;
    [SerializeField] private Button closeReviewButton;

    [Header("Tombol")]
    [SerializeField] private Button reviewPlanButton;

    [Header("Tab Rail")]
    [SerializeField] private bool autoWireTabs = true;
    [SerializeField] private Button tabGoals;
    [SerializeField] private Button tabStore;
    [SerializeField] private Button tabLedger;
    [SerializeField] private Button tabMarket;
    [SerializeField] private Button tabHours;
    [SerializeField] private Button tabSkills;
    [SerializeField] private Button tabNotes;
    [SerializeField] private Button tabQuest;

    [Header("Halaman Awal")]
    [SerializeField] private PlannerPageId startPage = PlannerPageId.Goals;

    [Header("Setup Otomatis")]
    [Tooltip("Cari halaman, tab, header, dan modal yang sudah ada di scene.")]
    [SerializeField] private bool autoResolveSceneReferences = true;

    [Tooltip("Buat kartu dek dari Card_Prefab memakai PlannerCatalog.")]
    [SerializeField] private bool buildDeckFromCatalog = true;

    [Tooltip("Jika scene dibuka langsung tanpa CoreLoop, buat sistem gameplay di runtime.")]
    [SerializeField] private bool createSystemsIfMissing = true;

    private DropSlot selectedSlot;
    private bool wired;

    public PlannerBook Book => book;
    public CoreLoop CoreLoop => coreLoop;
    public DropSlot[] Slots => slots;
    public DraggableCard CardPrefab => cardPrefab;
    public bool BuildSavedCards => buildSavedCards;

    private void Awake()
    {
        Resolve();
        if (autoResolveSceneReferences) ResolveSceneReferences();
        CollectSlots();
        WireControls();
        HideReviewModal();
        SwitchPage(startPage);
        RefreshAll();
    }

    private void EnsureRuntimeSystems()
    {
        if (!createSystemsIfMissing) return;

        if (CoreLoop.Instance != null)
        {
            if (PlannerBook.Instance == null)
            {
                GameObject bookObject = new GameObject("Runtime Planner Book");
                bookObject.AddComponent<PlannerBook>();
            }
            return;
        }

        GameObject systems = new GameObject("Runtime GameSystems");
        CoreLoop loop = systems.AddComponent<CoreLoop>();
        systems.AddComponent<PlannerBook>();
        loop.StartNewGame();
    }

    private void ResolveSceneReferences()
    {
        if (goalsPage == null) goalsPage = FindObject("BookArea/PagesHost/GoalsPage");
        if (storePage == null) storePage = FindObject("BookArea/PagesHost/StorePage");
        if (ledgerPage == null) ledgerPage = FindObject("BookArea/PagesHost/LedgerPage");
        if (marketPage == null) marketPage = FindObject("BookArea/PagesHost/MarketPage");
        if (hoursPage == null) hoursPage = FindObject("BookArea/PagesHost/HoursPage");
        if (skillPage == null) skillPage = FindObject("BookArea/PagesHost/SkillsPage");
        if (notesPage == null) notesPage = FindObject("BookArea/PagesHost/NotesPage");
        if (questPage == null) questPage = FindObject("BookArea/PagesHost/QuestPage");

        if (weekLabel == null) weekLabel = FindText("Header/Stat_CALENDAR/Value");
        if (goldLabel == null) goldLabel = FindText("Header/Stat_GOLD/Value");
        if (debtLabel == null) debtLabel = FindText("Header/Stat_DEBT/Value");
        if (reviewPlanButton == null) reviewPlanButton = FindButton("Header/ReviewButton");

        if (reviewModal == null) reviewModal = FindObject("ReviewModal");
        if (reviewSummaryLabel == null) reviewSummaryLabel = FindText("ReviewModal/Panel/SummaryViewport/Summary");
        if (reviewWarningLabel == null) reviewWarningLabel = FindText("ReviewModal/Panel/WarningBox/Text");
        if (startAnywayButton == null) startAnywayButton = FindButton("ReviewModal/Panel/StartAnyway");
        if (keepPlanningButton == null) keepPlanningButton = FindButton("ReviewModal/Panel/KeepPlanning");

        if (goalCountLabel == null) goalCountLabel = FindText("BookArea/PagesHost/GoalsPage/BookCover/Pages/LeftPage/GoalCount");
        if (deckContainer == null)
        {
            GameObject cards = FindObject("BookArea/PagesHost/GoalsPage/BookCover/Pages/RightPage/CardsView/Viewport/CardsContainer");
            if (cards != null) deckContainer = cards.transform;
        }

        if (tabGoals == null) tabGoals = FindButton("BookArea/TabRail/Tab_Goals");
        if (tabStore == null) tabStore = FindButton("BookArea/TabRail/Tab_Store");
        if (tabLedger == null) tabLedger = FindButton("BookArea/TabRail/Tab_Ledger");
        if (tabMarket == null) tabMarket = FindButton("BookArea/TabRail/Tab_Market");
        if (tabHours == null) tabHours = FindButton("BookArea/TabRail/Tab_Hours");
        if (tabSkills == null) tabSkills = FindButton("BookArea/TabRail/Tab_Skills");
        if (tabNotes == null) tabNotes = FindButton("BookArea/TabRail/Tab_Notes");
        if (tabQuest == null) tabQuest = FindButton("BookArea/TabRail/Tab_Quest");
    }

    private GameObject FindObject(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found.gameObject : null;
    }

    private TMP_Text FindText(string path)
    {
        GameObject found = FindObject(path);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private Button FindButton(string path)
    {
        GameObject found = FindObject(path);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private void BuildDeckFromCatalog()
    {
        if (!buildDeckFromCatalog || cardPrefab == null || deckContainer == null) return;
        if (deckContainer.GetComponentsInChildren<DraggableCard>(true).Length > 0)
        {
            LayoutDeckCards();
            return;
        }

        System.Collections.Generic.List<PlannerGoalCardData> goals = PlannerCatalog.GoalCards();
        for (int i = 0; i < goals.Count; i++)
        {
            DraggableCard card = Instantiate(cardPrefab, deckContainer);
            card.name = "Deck_" + goals[i].activityName.Replace(" ", string.Empty);
            card.gameObject.SetActive(true);
            card.SetDeckTemplate(true);
            card.SetActivity(goals[i]);
        }

        LayoutDeckCards();
    }

    /// <summary>
    /// Menata kartu dek secara manual supaya tidak bergantung pada
    /// VerticalLayoutGroup yang bisa menghasilkan bar kosong di Play mode.
    /// </summary>
    private void LayoutDeckCards()
    {
        if (deckContainer == null) return;
        RectTransform deckRect = deckContainer as RectTransform;
        if (deckRect == null) return;

        VerticalLayoutGroup group = deckContainer.GetComponent<VerticalLayoutGroup>();
        if (group != null) group.enabled = false;
        ContentSizeFitter fitter = deckContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        int count = 0;
        for (int i = 0; i < deckContainer.childCount; i++)
        {
            RectTransform child = deckContainer.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (child.GetComponent<DraggableCard>() == null) continue;

            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(1f, 1f);
            child.pivot = new Vector2(0.5f, 1f);
            child.anchoredPosition = new Vector2(0f, -count * 74f);
            child.sizeDelta = new Vector2(0f, 70f);
            count++;
        }

        deckRect.sizeDelta = new Vector2(0f, Mathf.Max(1, count) * 74f);
    }

    private void Start()
    {
        // CoreLoop dan PlannerBook sekarang ada di hierarchy (GameSystems),
        // bukan dibuat otomatis saat Play.
        if (coreLoop != null && coreLoop.Phase == GamePhase.MainMenu)
        {
            coreLoop.StartNewGame();
        }
    }

    private void OnEnable()
    {
        Resolve();
        if (autoResolveSceneReferences) ResolveSceneReferences();
        if (book != null) book.PlannerChanged += RefreshAll;
        if (coreLoop != null) coreLoop.StateChanged += RefreshHeader;
        RefreshAll();
    }

    private void OnDisable()
    {
        if (book != null) book.PlannerChanged -= RefreshAll;
        if (coreLoop != null) coreLoop.StateChanged -= RefreshHeader;
    }

    private void Resolve()
    {
        if (book == null) book = GetComponent<PlannerBook>();
        if (book == null) book = PlannerBook.Instance;
        if (coreLoop == null) coreLoop = GetComponent<CoreLoop>();
        if (coreLoop == null) coreLoop = CoreLoop.Instance;
    }

    private void CollectSlots()
    {
        if (slots != null && slots.Length > 0)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) slots[i].SetPlanner(this);
            }
            return;
        }

        slots = GetComponentsInChildren<DropSlot>(true);
        if (slots.Length > 0)
        {
            // Kalau baris hari hasil copas masih memakai indeks yang sama,
            // urutkan dari atas ke bawah lalu beri indeks 0-6.
            System.Collections.Generic.HashSet<int> unique = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < slots.Length; i++) unique.Add(slots[i].DayIndex);

            if (unique.Count < slots.Length)
            {
                Array.Sort(slots, (a, b) =>
                {
                    float ay = a.transform.position.y;
                    float by = b.transform.position.y;
                    return by.CompareTo(ay);
                });

                for (int i = 0; i < slots.Length; i++) slots[i].SetDayIndex(i);
            }
            else
            {
                Array.Sort(slots, (a, b) => a.DayIndex.CompareTo(b.DayIndex));
            }
        }

        for (int i = 0; i < slots.Length; i++) slots[i].SetPlanner(this);
    }

    private void WireControls()
    {
        if (wired) return;
        wired = true;

        Wire(reviewPlanButton, FinishPlan);
        Wire(startAnywayButton, StartWeekAnyway);
        Wire(keepPlanningButton, CloseReviewModal);
        Wire(closeReviewButton, CloseReviewModal);

        if (autoWireTabs)
        {
            Wire(tabGoals, () => SwitchPage(PlannerPageId.Goals));
            Wire(tabStore, () => SwitchPage(PlannerPageId.Store));
            Wire(tabLedger, () => SwitchPage(PlannerPageId.Ledger));
            Wire(tabMarket, () => SwitchPage(PlannerPageId.Market));
            Wire(tabHours, () => SwitchPage(PlannerPageId.Hours));
            Wire(tabSkills, () => SwitchPage(PlannerPageId.Skills));
            Wire(tabNotes, () => SwitchPage(PlannerPageId.Notes));
            Wire(tabQuest, () => SwitchPage(PlannerPageId.Quest));
        }
    }

    private void Wire(Button button, UnityAction action)
    {
        if (button == null || action == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    public void SwitchPage(PlannerPageId page)
    {
        switch (page)
        {
            case PlannerPageId.Store: base.SwitchPage("Store"); break;
            case PlannerPageId.Ledger: base.SwitchPage("Ledger"); break;
            case PlannerPageId.Market: base.SwitchPage("Market"); break;
            case PlannerPageId.Hours: base.SwitchPage("Hours"); break;
            case PlannerPageId.Skills: base.SwitchPage("Skill"); break;
            case PlannerPageId.Notes: base.SwitchPage("Notes"); break;
            case PlannerPageId.Quest: base.SwitchPage("Quest"); break;
            default: base.SwitchPage("Goals"); break;
        }
    }

    public void RefreshAll()
    {
        RefreshHeader();
        RefreshGoals();
    }

    public void RefreshHeader()
    {
        if (book == null) book = PlannerBook.Instance;
        if (coreLoop == null) coreLoop = CoreLoop.Instance;

        int dayIndex = coreLoop != null ? coreLoop.DayIndex : 0;
        int week = coreLoop != null ? coreLoop.Week : (book != null && book.CurrentWeek != null
            ? book.CurrentWeek.week : 1);
        string day = coreLoop != null ? ShortDay(coreLoop.DayName) : "MON";

        if (weekLabel != null) weekLabel.text = day + " | W" + week + " | D" + (dayIndex + 1);
        if (goldLabel != null) goldLabel.text = (coreLoop != null ? coreLoop.Gold : 0).ToString();
        if (debtLabel != null) debtLabel.text = (coreLoop != null ? coreLoop.Debt : 0).ToString();
        if (nextPaymentLabel != null)
        {
            nextPaymentLabel.text = (coreLoop != null ? coreLoop.MinimumDebtPayment : 450).ToString();
        }
        if (goldAfterLabel != null && book != null) goldAfterLabel.text = book.GoldAfterPlan.ToString();
        if (energyLabel != null && book != null) energyLabel.text = book.Energy.ToString();

        bool missing = book != null && book.MissingDayCount > 0;
        if (warningChip != null) warningChip.SetActive(missing);
        if (warningText != null)
        {
            warningText.text = missing
                ? book.MissingDayCount + " day(s) still need a goal"
                : "Your week is planned";
        }
    }

    public void RefreshGoals()
    {
        if (slots == null || slots.Length == 0) CollectSlots();
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) slots[i].RefreshFromPlanner();
            }
        }

        if (goalCountLabel != null && book != null)
        {
            goalCountLabel.text = book.PlannedDayCount + "/7";
        }
    }

    public DraggableCard CreateSavedCard(DropSlot slot, PlannerGoalCardData goal)
    {
        if (!buildSavedCards || slot == null || goal == null) return null;

        // Kartu slot sudah ditempatkan di scene oleh tools editor; pakai ulang
        // supaya tidak ada UI yang dibuat saat Play.
        DraggableCard card = slot.GetComponentInChildren<DraggableCard>(true);
        if (card == null)
        {
            if (cardPrefab == null) return null;
            card = Instantiate(cardPrefab, slot.transform);
        }

        card.transform.SetParent(slot.transform, false);
        card.SetDeckTemplate(false);
        card.name = "Card_" + goal.activityName;
        card.gameObject.SetActive(true);
        card.SetActivity(goal);
        card.CurrentSlot = slot;
        card.SetSlotPresentation(true);

        CanvasGroup group = card.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        return card;
    }

    public bool TryDropCard(DropSlot slot, DraggableCard card)
    {
        if (slot == null || card == null || book == null) return false;
        if (card.IsDeckTemplate)
        {
            PlannerGoalCardData goal = card.Activity;
            if (goal == null) return false;

            book.SetDayGoal(slot.DayIndex, goal);
            RefreshGoals();
            return true;
        }

        DropSlot oldSlot = card.CurrentSlot;
        if (oldSlot != null && oldSlot != slot)
        {
            // Lepas referensi dulu supaya event PlannerChanged tidak menghapus
            // kartu yang sedang dipindahkan.
            oldSlot.CurrentCard = null;
            oldSlot.RefreshEmptyLabel();
            book.ClearDayGoal(oldSlot.DayIndex);
        }

        if (slot.CurrentCard != null && slot.CurrentCard != card)
        {
            ClearSlot(slot);
        }

        slot.SetCard(card);
        card.gameObject.SetActive(true);
        book.SetDayGoal(slot.DayIndex, card.Activity);
        RefreshGoals();
        return true;
    }

    public void ClearSlot(DropSlot slot)
    {
        if (slot == null) return;

        if (book != null) book.ClearDayGoal(slot.DayIndex);
        slot.ClearCard(true);
        RefreshGoals();
    }

    /// <summary>
    /// Kartu yang ditarik dari kotak hari kembali ke dek: goal hari itu
    /// dikosongkan dan kartu salinannya dihapus. Kartu template di dek tetap
    /// di tempatnya, jadi dek tidak pernah kehilangan kartu.
    /// </summary>
    public void ReturnCardToDeck(DraggableCard card)
    {
        if (card == null) return;

        if (card.IsDeckTemplate)
        {
            card.ReturnToParent();
            return;
        }

        DropSlot slot = card.CurrentSlot;
        if (slot != null)
        {
            if (book != null) book.ClearDayGoal(slot.DayIndex);
            slot.ClearCard(true);
        }
        else
        {
            card.ReturnToParent();
        }

        RefreshGoals();
    }

    public void SelectDay(DropSlot slot)
    {
        if (selectedSlot != null) selectedSlot.Select(false);
        selectedSlot = slot;
        if (selectedSlot != null) selectedSlot.Select(true);
    }

    public void ReviewPlan()
    {
        if (book == null) book = PlannerBook.Instance;
        if (reviewModal != null) reviewModal.SetActive(true);
        if (reviewSummaryLabel != null && book != null) reviewSummaryLabel.text = book.PlanSummary();
        if (reviewWarningLabel != null && book != null)
        {
            reviewWarningLabel.text = book.MissingDayCount > 0
                ? "Some days have no goal yet."
                : "All seven days are planned.";
        }

        FillReviewSummary();
    }

    /// <summary>
    /// Lima baris ringkasan di ReviewModal: sales goal, herbs, living costs,
    /// perkiraan penjualan, dan gold setelah rencana dijalankan.
    /// </summary>
    private void FillReviewSummary()
    {
        if (book == null) return;

        int storeDays = 0;
        int marketDays = 0;
        int craftDays = 0;
        for (int day = 0; day < 7; day++)
        {
            PlannerDayPlan plan = book.GetDayPlan(day);
            if (plan == null || !plan.hasGoal || plan.goal == null) continue;

            switch (plan.goal.goalType)
            {
                case PlannerGoalType.OpenShop: storeDays++; break;
                case PlannerGoalType.MarketGuild: marketDays++; break;
                case PlannerGoalType.Craft: craftDays++; break;
            }
        }

        int salesGoal = storeDays * 12;
        int herbs = book.Budget.store;
        int living = book.Budget.personal;
        int salesGuess = Mathf.RoundToInt(storeDays * 30f + marketDays * 8f + craftDays * 12f);
        int goldAfter = book.Gold - living - herbs + salesGuess;

        if (reviewSalesGoalLabel != null) reviewSalesGoalLabel.text = salesGoal.ToString();
        if (reviewHerbsLabel != null) reviewHerbsLabel.text = "-" + herbs;
        if (reviewLivingLabel != null) reviewLivingLabel.text = "-" + living;
        if (reviewSalesGuessLabel != null) reviewSalesGuessLabel.text = "+" + salesGuess;
        if (reviewGoldAfterLabel != null) reviewGoldAfterLabel.text = "~ " + goldAfter;
    }

    public void CloseReviewModal()
    {
        HideReviewModal();
    }

    public void StartWeekAnyway()
    {
        HideReviewModal();
        BeginWeekAndGoToWorld();
    }

    /// <summary>
    /// Tombol "Finish plan" hanya membuka ReviewModal. Perpindahan ke
    /// CityOverWorld terjadi dari tombol "Begin the week" di modal itu.
    /// </summary>
    public void FinishPlan()
    {
        ReviewPlan();
    }

    /// <summary>
    /// Memulai minggu lalu langsung membuka CityOverWorld. Kalau CoreLoop
    /// sudah memuat scene-nya, pemuatan kedua dilewati.
    /// </summary>
    private void BeginWeekAndGoToWorld()
    {
        if (coreLoop == null) coreLoop = CoreLoop.Instance;
        if (coreLoop != null) coreLoop.BeginWeek();

        if (SceneManager.GetActiveScene().name != "CityOverWorld")
        {
            SceneManager.LoadScene("CityOverWorld");
        }
    }

    private void HideReviewModal()
    {
        if (reviewModal != null) reviewModal.SetActive(false);
    }

    private static string ShortDay(string day)
    {
        if (string.IsNullOrEmpty(day)) return string.Empty;
        return day.Substring(0, Mathf.Min(3, day.Length)).ToUpperInvariant();
    }
}
