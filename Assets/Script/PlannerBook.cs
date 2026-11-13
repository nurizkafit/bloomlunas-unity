using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menyimpan seluruh isi Buku Planner dan menyambungkannya ke CoreLoop.
/// Script ini hanya mengubah data dan memanggil event; tidak membuat Canvas,
/// tombol, atau kartu secara otomatis saat Play.
/// </summary>
[DefaultExecutionOrder(-90)]
public class PlannerBook : MonoBehaviour
{
    public static PlannerBook Instance { get; private set; }

    [Header("Sumber Data")]
    [SerializeField] private CoreLoop coreLoop;

    [Header("Rencana Minggu")]
    [SerializeField] private PlannerWeekPlan currentWeek = new PlannerWeekPlan();
    [SerializeField] private PlannerWeekPlan previousWeek;

    [Header("Store")]
    [SerializeField] private List<PlannerProductOffer> products = new List<PlannerProductOffer>();
    [SerializeField] private PlannerBudget budget = new PlannerBudget();

    [Header("Market")]
    [SerializeField] private List<PlannerMarketItem> marketItems = new List<PlannerMarketItem>();

    [Header("Inventaris")]
    [SerializeField] private List<PlannerInventoryItem> inventory = new List<PlannerInventoryItem>();

    [Header("Ledger")]
    [SerializeField] private List<PlannerLedgerEntry> ledger = new List<PlannerLedgerEntry>();
    [SerializeField] private int jarSpentStore;
    [SerializeField] private int jarSpentPersonal;
    [SerializeField] private int jarSpentBuffer;

    [Header("Skills")]
    [SerializeField] private List<PlannerSkillProgress> skills = new List<PlannerSkillProgress>();
    [SerializeField] private int skillXpPerGoal = 5;
    [SerializeField] private int skillXpPerLevel = 30;

    [Header("Notes")]
    [SerializeField] private string lastWeekNote =
        "Your first week. At the end of each week, you leave a note here for yourself.";
    [SerializeField] private List<PlannerFirstStep> firstSteps = new List<PlannerFirstStep>();

    [Header("Quests")]
    [SerializeField] private List<PlannerCommission> commissions = new List<PlannerCommission>();
    [SerializeField] private List<PlannerRequest> requests = new List<PlannerRequest>();

    [Header("Opening Hours")]
    [SerializeField] private List<PlannerScheduleEntry> schedule = new List<PlannerScheduleEntry>();

    [Header("Energi")]
    [SerializeField] private int energy = 6;
    [SerializeField] private int maxEnergy = 6;

    /// <summary>Dipanggil saat data planner berubah dan UI perlu refresh.</summary>
    public event Action PlannerChanged;

    /// <summary>Dipanggil saat planner pindah ke minggu/bulan baru.</summary>
    public event Action<PlannerWeekPlan> WeekChanged;

    /// <summary>Dipanggil saat satu goal hari berhasil diselesaikan.</summary>
    public event Action<PlannerDayPlan> DayGoalCompleted;

    public PlannerWeekPlan CurrentWeek => currentWeek;
    public PlannerWeekPlan PreviousWeek => previousWeek;
    public PlannerBudget Budget => budget;
    public int Energy => energy;
    public int MaxEnergy => maxEnergy;
    public string LastWeekNote => lastWeekNote;
    public IReadOnlyList<PlannerProductOffer> Products => products;
    public IReadOnlyList<PlannerMarketItem> MarketItems => marketItems;
    public IReadOnlyList<PlannerInventoryItem> Inventory => inventory;
    public IReadOnlyList<PlannerLedgerEntry> Ledger => ledger;
    public IReadOnlyList<PlannerSkillProgress> Skills => skills;
    public IReadOnlyList<PlannerFirstStep> FirstSteps => firstSteps;
    public IReadOnlyList<PlannerCommission> Commissions => commissions;
    public IReadOnlyList<PlannerRequest> Requests => requests;
    public IReadOnlyList<PlannerScheduleEntry> Schedule => schedule;

    public int WeeksPerMonth => coreLoop != null ? coreLoop.WeeksPerMonth : 4;
    public int WeeklyLivingCost => coreLoop != null ? coreLoop.WeeklyLivingCost : 40;
    public int MonthlyUtilities => coreLoop != null ? coreLoop.MonthlyUtilities : 120;
    public int MinimumDebtPayment => coreLoop != null ? coreLoop.MinimumDebtPayment : 450;
    public int Gold => coreLoop != null ? coreLoop.Gold : 0;
    public int Debt => coreLoop != null ? coreLoop.Debt : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        ResolveCoreLoop();
        InitializeData();
        EnsureWeek(coreLoop != null ? coreLoop.Month : 1, coreLoop != null ? coreLoop.Week : 1);
        Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this) Instance = null;
    }

    private void ResolveCoreLoop()
    {
        if (coreLoop == null) coreLoop = GetComponent<CoreLoop>();
        if (coreLoop == null) coreLoop = CoreLoop.Instance;
    }

    private void Subscribe()
    {
        if (coreLoop == null) return;
        coreLoop.StateChanged += HandleStateChanged;
        coreLoop.DayCompleted += HandleDayCompleted;
        coreLoop.NewGameStarted += HandleNewGame;
    }

    private void Unsubscribe()
    {
        if (coreLoop == null) return;
        coreLoop.StateChanged -= HandleStateChanged;
        coreLoop.DayCompleted -= HandleDayCompleted;
        coreLoop.NewGameStarted -= HandleNewGame;
    }

    private void InitializeData()
    {
        if (products == null || products.Count == 0) products = PlannerCatalog.Products();
        if (marketItems == null || marketItems.Count == 0) marketItems = PlannerCatalog.MarketItems();
        if (inventory == null || inventory.Count == 0) inventory = PlannerCatalog.StartingInventory();
        if (schedule == null || schedule.Count == 0) schedule = PlannerCatalog.Schedule();
        if (firstSteps == null || firstSteps.Count == 0) firstSteps = PlannerCatalog.FirstSteps();
        if (skills == null || skills.Count == 0)
        {
            skills = new List<PlannerSkillProgress>
            {
                new PlannerSkillProgress { branch = PlannerSkillBranch.Moon },
                new PlannerSkillProgress { branch = PlannerSkillBranch.Sun },
                new PlannerSkillProgress { branch = PlannerSkillBranch.Earth }
            };
        }

        if (budget == null) budget = new PlannerBudget();
        if (string.IsNullOrEmpty(lastWeekNote))
        {
            lastWeekNote = "Your first week. At the end of each week, you leave a note here for yourself.";
        }

        if (commissions == null) commissions = new List<PlannerCommission>();
        if (requests == null) requests = new List<PlannerRequest>();
        if (ledger == null) ledger = new List<PlannerLedgerEntry>();
    }

    private void HandleNewGame()
    {
        ResetForNewGame();
    }

    private void HandleStateChanged()
    {
        if (coreLoop == null) return;
        if (coreLoop.Phase == GamePhase.Planning)
        {
            EnsureWeek(coreLoop.Month, coreLoop.Week);
        }

        RaisePlannerChanged();
    }

    private void HandleDayCompleted(int dayIndex)
    {
        PlannerDayPlan day = GetDayPlan(dayIndex);
        if (day != null && day.hasGoal && !day.completed)
        {
            day.missed = true;
        }

        AdvanceRequests(dayIndex);
        RaisePlannerChanged();
    }

    // ---------------------------------------------------------------------
    // Rencana minggu
    // ---------------------------------------------------------------------

    public void EnsureWeek(int month, int week)
    {
        bool needsNewWeek = currentWeek == null ||
                            currentWeek.days == null ||
                            currentWeek.days.Count != 7 ||
                            currentWeek.month != month ||
                            currentWeek.week != week;

        if (!needsNewWeek) return;

        if (currentWeek != null && currentWeek.days != null && currentWeek.days.Count > 0)
        {
            previousWeek = currentWeek;
        }

        currentWeek = new PlannerWeekPlan
        {
            month = month,
            week = week,
            days = new List<PlannerDayPlan>()
        };

        for (int i = 0; i < 7; i++)
        {
            currentWeek.days.Add(new PlannerDayPlan { dayIndex = i });
        }

        WeekChanged?.Invoke(currentWeek);
        RaisePlannerChanged();
    }

    public PlannerDayPlan GetDayPlan(int dayIndex)
    {
        if (currentWeek == null || currentWeek.days == null) return null;
        if (dayIndex < 0 || dayIndex >= currentWeek.days.Count) return null;
        return currentWeek.days[dayIndex];
    }

    public PlannerGoalCardData GetDayGoal(int dayIndex)
    {
        PlannerDayPlan day = GetDayPlan(dayIndex);
        return day != null && day.hasGoal ? day.goal : null;
    }

    public bool SetDayGoal(int dayIndex, PlannerGoalCardData goal)
    {
        PlannerDayPlan day = GetDayPlan(dayIndex);
        if (day == null || goal == null) return false;

        day.hasGoal = true;
        day.goal = goal.Clone();
        day.completed = false;
        day.missed = false;
        MarkFirstStep(0, true);
        RaisePlannerChanged();
        return true;
    }

    public bool ClearDayGoal(int dayIndex)
    {
        PlannerDayPlan day = GetDayPlan(dayIndex);
        if (day == null) return false;

        day.hasGoal = false;
        day.goal = null;
        day.completed = false;
        day.missed = false;
        RaisePlannerChanged();
        return true;
    }

    public bool CompleteGoal(int dayIndex)
    {
        PlannerDayPlan day = GetDayPlan(dayIndex);
        if (day == null || !day.hasGoal || day.completed) return false;

        day.completed = true;
        day.missed = false;

        if (day.goal != null && day.goal.skillBranch != PlannerSkillBranch.None)
        {
            AddSkillXp(day.goal.skillBranch, skillXpPerGoal);
        }

        AddLedger("Goal: " + (day.goal != null ? day.goal.activityName : "Day goal"),
            dayIndex, 0, PlannerLedgerKind.Neutral);
        MarkFirstStep(0, true);
        DayGoalCompleted?.Invoke(day);
        RaisePlannerChanged();
        return true;
    }

    public bool CompleteCurrentGoal()
    {
        return coreLoop != null && CompleteGoal(coreLoop.DayIndex);
    }

    public int PlannedDayCount
    {
        get
        {
            int count = 0;
            if (currentWeek == null || currentWeek.days == null) return count;
            for (int i = 0; i < currentWeek.days.Count; i++)
            {
                if (currentWeek.days[i].hasGoal) count++;
            }
            return count;
        }
    }

    public int MissingDayCount => 7 - PlannedDayCount;

    public bool HasCompletePlan => PlannedDayCount >= 7;

    public List<int> MissingDays()
    {
        List<int> result = new List<int>();
        for (int i = 0; i < 7; i++)
        {
            PlannerDayPlan day = GetDayPlan(i);
            if (day == null || !day.hasGoal) result.Add(i);
        }
        return result;
    }

    public string PlanSummary()
    {
        string text = "WEEK " + (currentWeek != null ? currentWeek.week : 0) + " PLAN\n";
        for (int i = 0; i < 7; i++)
        {
            PlannerDayPlan day = GetDayPlan(i);
            string name = CoreLoop.DayNames[i];
            if (day == null || !day.hasGoal)
            {
                text += name + "  -  no goal yet\n";
            }
            else
            {
                string status = day.completed ? "  [done]" : day.missed ? "  [missed]" : "";
                text += name + "  -  " + day.goal.activityName + status + "\n";
            }
        }

        if (MissingDayCount > 0)
        {
            text += "\n" + MissingDayCount + " day(s) still need a goal.";
        }
        else
        {
            text += "\nAll seven days are planned.";
        }

        return text;
    }

    // ---------------------------------------------------------------------
    // Store dan budget
    // ---------------------------------------------------------------------

    public PlannerProductOffer GetProduct(string productId)
    {
        if (products == null) return null;
        for (int i = 0; i < products.Count; i++)
        {
            if (products[i].productId == productId) return products[i];
        }
        return null;
    }

    public bool SetProductUnits(string productId, int units)
    {
        PlannerProductOffer product = GetProduct(productId);
        if (product == null) return false;

        product.units = Mathf.Max(0, units);
        if (product.units > 0) MarkFirstStep(1, true);
        RaisePlannerChanged();
        return true;
    }

    public bool ChangeProductUnits(string productId, int delta)
    {
        PlannerProductOffer product = GetProduct(productId);
        return product != null && SetProductUnits(productId, product.units + delta);
    }

    public int TotalProductUnits
    {
        get
        {
            int total = 0;
            if (products == null) return total;
            for (int i = 0; i < products.Count; i++) total += products[i].units;
            return total;
        }
    }

    public int GetProductRevenue(string productId)
    {
        PlannerProductOffer product = GetProduct(productId);
        if (product == null) return 0;
        return Mathf.RoundToInt((float)GetPrice(productId) * product.units);
    }

    public int ProjectedStoreRevenue
    {
        get
        {
            int total = 0;
            if (products == null) return total;
            for (int i = 0; i < products.Count; i++)
            {
                total += Mathf.RoundToInt((float)GetPrice(products[i].productId) * products[i].units);
            }
            return total;
        }
    }

    public int JarTotal => budget != null ? budget.Total : 0;

    public int JarRemaining => Mathf.Max(0, Gold - JarTotal);

    public int GoldAfterPlan => Mathf.Max(0, Gold - WeeklyLivingCost);

    public bool SetJar(PlannerJarKind kind, int value)
    {
        if (budget == null) return false;

        int current = kind == PlannerJarKind.Store ? budget.store :
            kind == PlannerJarKind.Personal ? budget.personal : budget.buffer;
        int other = budget.Total - current;
        int max = Mathf.Max(0, Gold - other);
        value = Mathf.Clamp(value, 0, max);

        switch (kind)
        {
            case PlannerJarKind.Store: budget.store = value; break;
            case PlannerJarKind.Personal: budget.personal = value; break;
            case PlannerJarKind.Buffer: budget.buffer = value; break;
        }

        MarkFirstStep(2, true);
        RaisePlannerChanged();
        return true;
    }

    public bool ChangeJar(PlannerJarKind kind, int delta)
    {
        if (budget == null) return false;
        int current = kind == PlannerJarKind.Store ? budget.store :
            kind == PlannerJarKind.Personal ? budget.personal : budget.buffer;
        return SetJar(kind, current + delta);
    }

    public bool SpendFromJar(PlannerJarKind kind, int amount)
    {
        if (amount <= 0 || coreLoop == null) return false;

        int available = kind == PlannerJarKind.Store ? budget.store :
            kind == PlannerJarKind.Personal ? budget.personal : budget.buffer;
        if (available < amount) return false;

        if (!coreLoop.SpendGold(amount)) return false;

        switch (kind)
        {
            case PlannerJarKind.Store: budget.store -= amount; jarSpentStore += amount; break;
            case PlannerJarKind.Personal: budget.personal -= amount; jarSpentPersonal += amount; break;
            case PlannerJarKind.Buffer: budget.buffer -= amount; jarSpentBuffer += amount; break;
        }

        RaisePlannerChanged();
        return true;
    }

    public int JarSpent(PlannerJarKind kind)
    {
        return kind == PlannerJarKind.Store ? jarSpentStore :
            kind == PlannerJarKind.Personal ? jarSpentPersonal : jarSpentBuffer;
    }

    // ---------------------------------------------------------------------
    // Market
    // ---------------------------------------------------------------------

    public PlannerMarketItem GetMarketItem(string itemId)
    {
        if (marketItems == null) return null;
        for (int i = 0; i < marketItems.Count; i++)
        {
            if (marketItems[i].itemId == itemId) return marketItems[i];
        }
        return null;
    }

    public bool SetPriceBand(string itemId, PlannerPriceBand band)
    {
        PlannerMarketItem item = GetMarketItem(itemId);
        if (item == null) return false;

        item.priceBand = band;
        item.nowPrice = item.usualPrice * BandMultiplier(band);
        RaisePlannerChanged();
        return true;
    }

    public bool SetPriceBand(string itemId, int bandIndex)
    {
        if (bandIndex < 0 || bandIndex > 2) return false;
        return SetPriceBand(itemId, (PlannerPriceBand)bandIndex);
    }

    public float GetPrice(string itemId)
    {
        PlannerMarketItem item = GetMarketItem(itemId);
        if (item == null) return 0f;
        return Mathf.Round(item.usualPrice * BandMultiplier(item.priceBand) * 10f) / 10f;
    }

    public static float BandMultiplier(PlannerPriceBand band)
    {
        switch (band)
        {
            case PlannerPriceBand.Low: return 0.8f;
            case PlannerPriceBand.High: return 1.25f;
            default: return 1f;
        }
    }

    public static float BandDemandChance(PlannerPriceBand band)
    {
        switch (band)
        {
            case PlannerPriceBand.Low: return 1.25f;
            case PlannerPriceBand.High: return 0.6f;
            default: return 1f;
        }
    }

    // ---------------------------------------------------------------------
    // Inventaris, jual, beli
    // ---------------------------------------------------------------------

    public int GetAmount(string itemId)
    {
        PlannerInventoryItem item = GetInventoryItem(itemId);
        return item != null ? item.amount : 0;
    }

    public PlannerInventoryItem GetInventoryItem(string itemId)
    {
        if (inventory == null) return null;
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].itemId == itemId) return inventory[i];
        }
        return null;
    }

    public bool AddInventory(string itemId, string displayName, int amount, bool isProduct)
    {
        if (string.IsNullOrEmpty(itemId) || amount == 0) return false;

        PlannerInventoryItem item = GetInventoryItem(itemId);
        if (item == null)
        {
            item = new PlannerInventoryItem
            {
                itemId = itemId,
                displayName = displayName,
                amount = 0,
                isProduct = isProduct
            };
            inventory.Add(item);
        }

        item.amount = Mathf.Max(0, item.amount + amount);
        RaisePlannerChanged();
        return true;
    }

    /// <summary>Harga jual di market stall: 60% dari harga toko (prototype).</summary>
    public float GetSellPrice(string itemId)
    {
        return Mathf.Round(GetPrice(itemId) * 0.6f * 10f) / 10f;
    }

    public bool SellProduct(string productId, int units)
    {
        if (units <= 0 || coreLoop == null) return false;

        PlannerInventoryItem stock = GetInventoryItem(productId);
        if (stock == null || stock.amount < units) return false;

        PlannerMarketItem market = GetMarketItem(productId);
        string name = market != null ? market.displayName : productId;
        int amount = Mathf.RoundToInt(GetSellPrice(productId) * units);

        stock.amount -= units;
        coreLoop.RecordSale(amount, units);
        AddLedger("Sold " + units + " " + name, coreLoop.DayIndex, amount, PlannerLedgerKind.Earn);
        MarkFirstStep(6, true);
        RaisePlannerChanged();
        return true;
    }

    public bool BuyMaterial(string itemId, int units)
    {
        if (units <= 0 || coreLoop == null) return false;

        PlannerMarketItem market = GetMarketItem(itemId);
        if (market == null) return false;

        int cost = Mathf.RoundToInt(GetPrice(itemId) * units);
        if (!coreLoop.SpendGold(cost)) return false;

        AddInventory(itemId, market.displayName, units, false);
        AddLedger("Bought " + units + " " + market.displayName, coreLoop.DayIndex, cost,
            PlannerLedgerKind.Spend);
        MarkFirstStep(3, true);
        RaisePlannerChanged();
        return true;
    }

    // ---------------------------------------------------------------------
    // Ledger
    // ---------------------------------------------------------------------

    public void AddLedger(string description, int dayIndex, int amount, PlannerLedgerKind kind)
    {
        if (ledger == null) ledger = new List<PlannerLedgerEntry>();

        int week = coreLoop != null ? coreLoop.Week : (currentWeek != null ? currentWeek.week : 1);
        int month = coreLoop != null ? coreLoop.Month : 1;
        ledger.Add(new PlannerLedgerEntry
        {
            weekLabel = "M" + month + " W" + week,
            dayIndex = dayIndex,
            description = description,
            amount = amount,
            kind = kind
        });

        RaisePlannerChanged();
    }

    public int WeekEarned
    {
        get
        {
            int total = 0;
            if (ledger == null) return total;
            string weekLabel = CurrentWeekLabel;
            for (int i = 0; i < ledger.Count; i++)
            {
                if (ledger[i].weekLabel == weekLabel && ledger[i].kind == PlannerLedgerKind.Earn)
                {
                    total += ledger[i].amount;
                }
            }
            return total;
        }
    }

    public int WeekSpent
    {
        get
        {
            int total = 0;
            if (ledger == null) return total;
            string weekLabel = CurrentWeekLabel;
            for (int i = 0; i < ledger.Count; i++)
            {
                if (ledger[i].weekLabel == weekLabel && ledger[i].kind == PlannerLedgerKind.Spend)
                {
                    total += ledger[i].amount;
                }
            }
            return total;
        }
    }

    private string CurrentWeekLabel
    {
        get
        {
            int month = coreLoop != null ? coreLoop.Month : 1;
            int week = coreLoop != null ? coreLoop.Week : (currentWeek != null ? currentWeek.week : 1);
            return "M" + month + " W" + week;
        }
    }

    // ---------------------------------------------------------------------
    // Skills
    // ---------------------------------------------------------------------

    public PlannerSkillProgress GetSkill(PlannerSkillBranch branch)
    {
        if (skills == null) return null;
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].branch == branch) return skills[i];
        }
        return null;
    }

    public int GetSkillLevel(PlannerSkillBranch branch)
    {
        PlannerSkillProgress skill = GetSkill(branch);
        return skill != null ? skill.level : 0;
    }

    public int GetSkillXp(PlannerSkillBranch branch)
    {
        PlannerSkillProgress skill = GetSkill(branch);
        return skill != null ? skill.xp : 0;
    }

    public int SkillXpPerGoal => Mathf.Max(0, skillXpPerGoal);
    public int SkillXpForNextLevel => Mathf.Max(1, skillXpPerLevel);

    public void AddSkillXp(PlannerSkillBranch branch, int amount)
    {
        if (branch == PlannerSkillBranch.None || amount <= 0) return;

        PlannerSkillProgress skill = GetSkill(branch);
        if (skill == null)
        {
            skill = new PlannerSkillProgress { branch = branch };
            skills.Add(skill);
        }

        skill.xp += amount;
        while (skill.xp >= skillXpPerLevel)
        {
            skill.xp -= skillXpPerLevel;
            skill.level++;
        }

        RaisePlannerChanged();
    }

    public float MoonPriceMultiplier => GetSkillLevel(PlannerSkillBranch.Moon) >= 3 ? 1.15f : 1f;
    public float SunCustomerBonus => GetSkillLevel(PlannerSkillBranch.Sun) >= 2 ? 0.08f : 0f;
    public float EarthHerbBonus => GetSkillLevel(PlannerSkillBranch.Earth) >= 2 ? 0.15f : 0f;

    // ---------------------------------------------------------------------
    // Notes
    // ---------------------------------------------------------------------

    public void SetLastWeekNote(string note)
    {
        lastWeekNote = note ?? string.Empty;
        RaisePlannerChanged();
    }

    public void SetFirstStep(int index, bool done)
    {
        if (firstSteps == null || index < 0 || index >= firstSteps.Count) return;
        firstSteps[index].done = done;
        RaisePlannerChanged();
    }

    public void ToggleFirstStep(int index)
    {
        if (firstSteps == null || index < 0 || index >= firstSteps.Count) return;
        firstSteps[index].done = !firstSteps[index].done;
        RaisePlannerChanged();
    }

    public void MarkFirstStep(int index, bool done)
    {
        if (firstSteps == null || index < 0 || index >= firstSteps.Count) return;
        if (firstSteps[index].done == done) return;
        firstSteps[index].done = done;
    }

    public int CompletedFirstSteps
    {
        get
        {
            int count = 0;
            if (firstSteps == null) return count;
            for (int i = 0; i < firstSteps.Count; i++)
            {
                if (firstSteps[i].done) count++;
            }
            return count;
        }
    }

    // ---------------------------------------------------------------------
    // Quest board
    // ---------------------------------------------------------------------

    public void TakeCommission(PlannerCommission commission)
    {
        if (commission == null) return;
        if (commissions == null) commissions = new List<PlannerCommission>();

        for (int i = 0; i < commissions.Count; i++)
        {
            if (commissions[i].commissionId == commission.commissionId)
            {
                commissions[i].status = PlannerCommissionStatus.Taken;
                RaisePlannerChanged();
                return;
            }
        }

        commission.status = PlannerCommissionStatus.Taken;
        commissions.Add(commission);
        MarkFirstStep(8, true);
        RaisePlannerChanged();
    }

    public bool DeliverCommission(string commissionId)
    {
        if (commissions == null) return false;

        for (int i = 0; i < commissions.Count; i++)
        {
            PlannerCommission commission = commissions[i];
            if (commission.commissionId != commissionId) continue;
            if (commission.status != PlannerCommissionStatus.Taken &&
                commission.status != PlannerCommissionStatus.Ready) return false;

            PlannerInventoryItem item = GetInventoryItem(commission.itemId);
            if (item == null || item.amount < commission.required) return false;

            item.amount -= commission.required;
            commission.status = PlannerCommissionStatus.Delivered;
            if (coreLoop != null)
            {
                coreLoop.AddGold(commission.reward);
                AddLedger("Commission: " + commission.title, coreLoop.DayIndex,
                    commission.reward, PlannerLedgerKind.Earn);
            }

            MarkFirstStep(8, true);
            RaisePlannerChanged();
            return true;
        }

        return false;
    }

    public void PostRequest(PlannerRequest request)
    {
        if (request == null) return;
        if (requests == null) requests = new List<PlannerRequest>();

        for (int i = 0; i < requests.Count; i++)
        {
            if (requests[i].requestId == request.requestId) return;
        }

        requests.Add(request);
        RaisePlannerChanged();
    }

    public void AdvanceRequests(int dayIndex)
    {
        if (requests == null) return;
        for (int i = 0; i < requests.Count; i++)
        {
            PlannerRequest request = requests[i];
            if (request.status == PlannerRequestStatus.Posted && request.arrivalDay <= dayIndex)
            {
                request.status = PlannerRequestStatus.Arrived;
            }
        }
    }

    public bool ClaimRequest(string requestId)
    {
        if (requests == null) return false;
        for (int i = 0; i < requests.Count; i++)
        {
            PlannerRequest request = requests[i];
            if (request.requestId != requestId) continue;
            if (request.status != PlannerRequestStatus.Arrived) return false;

            request.status = PlannerRequestStatus.Claimed;
            if (coreLoop != null) coreLoop.AddGold(request.reward);
            RaisePlannerChanged();
            return true;
        }
        return false;
    }

    // ---------------------------------------------------------------------
    // Opening hours
    // ---------------------------------------------------------------------

    public PlannerScheduleEntry GetSchedule(string locationName)
    {
        if (schedule == null) return null;
        for (int i = 0; i < schedule.Count; i++)
        {
            if (schedule[i].locationName == locationName) return schedule[i];
        }
        return null;
    }

    public bool IsLocationOpen(string locationName, int dayIndex, int hour)
    {
        PlannerScheduleEntry entry = GetSchedule(locationName);
        if (entry == null) return false;
        if (entry.closedDay == dayIndex) return false;
        if (entry.openHour == 0 && entry.closeHour >= 24) return true;
        return hour >= entry.openHour && hour < entry.closeHour;
    }

    public int GetWalkMinutes(string locationName)
    {
        PlannerScheduleEntry entry = GetSchedule(locationName);
        return entry != null ? entry.walkMinutes : 0;
    }

    public void SetEnergy(int value)
    {
        energy = Mathf.Clamp(value, 0, Mathf.Max(1, maxEnergy));
        RaisePlannerChanged();
    }

    // ---------------------------------------------------------------------
    // Reset
    // ---------------------------------------------------------------------

    public void ResetForNewGame()
    {
        products = PlannerCatalog.Products();
        marketItems = PlannerCatalog.MarketItems();
        inventory = PlannerCatalog.StartingInventory();
        schedule = PlannerCatalog.Schedule();
        firstSteps = PlannerCatalog.FirstSteps();
        skills = new List<PlannerSkillProgress>
        {
            new PlannerSkillProgress { branch = PlannerSkillBranch.Moon },
            new PlannerSkillProgress { branch = PlannerSkillBranch.Sun },
            new PlannerSkillProgress { branch = PlannerSkillBranch.Earth }
        };
        budget = new PlannerBudget();
        ledger = new List<PlannerLedgerEntry>();
        jarSpentStore = 0;
        jarSpentPersonal = 0;
        jarSpentBuffer = 0;
        lastWeekNote = "Your first week. At the end of each week, you leave a note here for yourself.";
        commissions = new List<PlannerCommission>();
        requests = new List<PlannerRequest>();
        energy = maxEnergy;
        previousWeek = null;
        currentWeek = null;
        EnsureWeek(1, 1);
        RaisePlannerChanged();
    }

    public void RaisePlannerChanged()
    {
        PlannerChanged?.Invoke();
    }
}
