using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fase besar permainan. Act memakai <see cref="MonthEnd"/>,
/// Retro memakai <see cref="Retro"/>.
/// </summary>
public enum GamePhase
{
    MainMenu,
    Planning,
    Playing,
    DayReview,
    WeekReview,
    MonthEnd,
    Retro,
    Ending
}

/// <summary>
/// Rekap yang dibuat saat bulan ditutup. Dipakai oleh layar Act/Retro.
/// Kelas ini hanya data; tidak membuat UI.
/// </summary>
[Serializable]
public class MonthReport
{
    public int month;
    public int goldAtStart;
    public int goldAtEnd;
    public int debtAtStart;
    public int debtAtEnd;
    public int goldEarned;
    public int goldSpent;
    public int debtPaid;
    public int livingPaid;
    public int utilitiesPaid;
    public int productsSold;
    public int daysPlayed;
    public bool debtCleared;
    public string summary;
}

/// <summary>
/// Inti putaran Bloom Lunas:
/// Main Menu -> Planning -> Playing (7 hari) -> Week Review
/// -> Planning minggu berikutnya, atau Month End (Act) -> Retro
/// -> Planning bulan berikutnya.
///
/// Script ini sengaja tidak membuat GameObject, Canvas, tombol, atau panel.
/// Semua tampilan dan perpindahan scene diatur manual oleh pemain di Unity.
/// </summary>
[DefaultExecutionOrder(-100)]
public class CoreLoop : MonoBehaviour
{
    public static CoreLoop Instance { get; private set; }

    public static readonly string[] DayNames =
    {
        "Monday", "Tuesday", "Wednesday", "Thursday",
        "Friday", "Saturday", "Sunday"
    };

    [Header("Persistensi")]
    [Tooltip("Centang agar CoreLoop tidak hilang saat pindah scene.")]
    [SerializeField] private bool persistBetweenScenes = true;

    [Header("Kalender")]
    [Tooltip("Jumlah minggu dalam satu bulan sebelum Act dibuka.")]
    [SerializeField, Min(1)] private int weeksPerMonth = 4;

    [Header("Ekonomi Awal")]
    [SerializeField] private int startingGold = 240;
    [SerializeField] private int startingDebt = 3000;

    [Header("Biaya Tetap")]
    [Tooltip("Dipotong otomatis setiap minggu selesai.")]
    [SerializeField] private int weeklyLivingCost = 40;

    [Tooltip("Dibayar pada Act bulanan. Kekurangan otomatis masuk ke utang.")]
    [SerializeField] private int monthlyUtilities = 120;

    [Tooltip("Nilai awal stepper pembayaran utang di Act.")]
    [SerializeField] private int minimumDebtPayment = 450;

    [Header("Perpindahan Scene (Opsional)")]
    [Tooltip("Jika mati, CoreLoop hanya mengubah data. Kamu yang memanggil LoadScene.")]
    [SerializeField] private bool loadScenes = false;
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string planningSceneName = "MulaiGame";
    [SerializeField] private string worldSceneName = "CityOverWorld";

    [Header("Status Runtime")]
    [SerializeField] private GamePhase phase = GamePhase.MainMenu;
    [SerializeField] private int month = 1;
    [SerializeField] private int week = 1;
    [SerializeField] private int dayIndex;
    [SerializeField] private int gold;
    [SerializeField] private int debt;

    [SerializeField] private int monthGoldEarned;
    [SerializeField] private int monthGoldSpent;
    [SerializeField] private int monthDebtPaid;
    [SerializeField] private int monthLivingPaid;
    [SerializeField] private int monthUtilitiesPaid;
    [SerializeField] private int monthProductsSold;
    [SerializeField] private int monthDaysPlayed;
    [SerializeField] private int monthStartGold;
    [SerializeField] private int monthStartDebt;

    private MonthReport lastMonthReport;

    /// <summary>Dipanggil setiap fase, hari, atau nilai ekonomi berubah.</summary>
    public event Action StateChanged;

    /// <summary>Dipanggil setelah Act selesai dan laporan bulan dibuat.</summary>
    public event Action<MonthReport> MonthClosed;

    /// <summary>Dipanggil saat New Game dimulai, sebelum UI planner memuat minggu pertama.</summary>
    public event Action NewGameStarted;

    /// <summary>Dipanggil saat satu hari ditutup. Nilainya index hari 0-6.</summary>
    public event Action<int> DayCompleted;

    public GamePhase Phase => phase;
    public int Month => month;
    public int Week => week;
    public int DayIndex => dayIndex;
    public string DayName => DayNames[Mathf.Clamp(dayIndex, 0, DayNames.Length - 1)];
    public int Gold => gold;
    public int Debt => debt;

    public int WeeksPerMonth => Mathf.Max(1, weeksPerMonth);
    public int TotalWeek => (month - 1) * WeeksPerMonth + week;
    public int WeeklyLivingCost => weeklyLivingCost;
    public int MonthlyUtilities => monthlyUtilities;
    public int MinimumDebtPayment => minimumDebtPayment;

    public int MonthGoldEarned => monthGoldEarned;
    public int MonthGoldSpent => monthGoldSpent;
    public int MonthDebtPaid => monthDebtPaid;
    public int MonthProductsSold => monthProductsSold;
    public int MonthDaysPlayed => monthDaysPlayed;

    public MonthReport LastMonthReport => lastMonthReport;

    public bool IsPlanning => phase == GamePhase.Planning;
    public bool IsPlaying => phase == GamePhase.Playing;
    public bool IsDayReview => phase == GamePhase.DayReview;
    public bool IsWeekReview => phase == GamePhase.WeekReview;
    public bool IsMonthEnd => phase == GamePhase.MonthEnd;
    public bool IsRetro => phase == GamePhase.Retro;
    public bool IsFinished => phase == GamePhase.Ending;
    public bool IsLastDayOfWeek => dayIndex >= 6;
    public bool IsLastWeekOfMonth => week >= WeeksPerMonth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistBetweenScenes && Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        weeksPerMonth = Mathf.Max(1, weeksPerMonth);
        startingGold = Mathf.Max(0, startingGold);
        startingDebt = Mathf.Max(0, startingDebt);
        weeklyLivingCost = Mathf.Max(0, weeklyLivingCost);
        monthlyUtilities = Mathf.Max(0, monthlyUtilities);
        minimumDebtPayment = Mathf.Max(0, minimumDebtPayment);
        dayIndex = Mathf.Clamp(dayIndex, 0, 6);
        week = Mathf.Clamp(week, 1, weeksPerMonth);
        month = Mathf.Max(1, month);
        gold = Mathf.Max(0, gold);
        debt = Mathf.Max(0, debt);
    }

    // ---------------------------------------------------------------------
    // Alur utama
    // ---------------------------------------------------------------------

    /// <summary>
    /// Memulai permainan dari awal. Panggil dari tombol New Game/Main Menu.
    /// </summary>
    public void StartNewGame()
    {
        month = 1;
        week = 1;
        dayIndex = 0;
        gold = Mathf.Max(0, startingGold);
        debt = Mathf.Max(0, startingDebt);
        lastMonthReport = null;

        ResetMonthStats();
        SetPhase(GamePhase.Planning);
        NewGameStarted?.Invoke();
        NotifyStateChanged();
        LoadSceneIfEnabled(planningSceneName);
    }

    /// <summary>
    /// Selesai menyusun rencana minggu, lalu masuk ke dunia.
    /// Panggil dari tombol "Start week" di buku planner.
    /// </summary>
    public void BeginWeek()
    {
        if (phase != GamePhase.Planning) return;

        dayIndex = 0;
        SetPhase(GamePhase.Playing);
        NotifyStateChanged();
        LoadSceneIfEnabled(worldSceneName);
    }

    /// <summary>
    /// Menutup hari yang sedang berjalan. Panggil dari kasur/tombol End day.
    /// Hari 1-6 masuk ke DayReview; hari ke-7 masuk ke WeekReview.
    /// </summary>
    public void CompleteDay()
    {
        if (phase != GamePhase.Playing) return;

        monthDaysPlayed++;
        DayCompleted?.Invoke(dayIndex);

        if (dayIndex < 6)
        {
            SetPhase(GamePhase.DayReview);
        }
        else
        {
            PayWeeklyLivingCost();
            SetPhase(GamePhase.WeekReview);
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Melanjutkan setelah panel Check hari/minggu.
    /// Hari berikutnya masuk ke Playing; minggu berikutnya masuk ke Planning.
    /// </summary>
    public void ContinueAfterReview()
    {
        if (phase == GamePhase.DayReview)
        {
            dayIndex++;
            SetPhase(GamePhase.Playing);
            NotifyStateChanged();
            LoadSceneIfEnabled(worldSceneName);
            return;
        }

        if (phase == GamePhase.WeekReview)
        {
            ContinueAfterWeek();
        }
    }

    /// <summary>
    /// Menutup minggu. Minggu 1-3 kembali ke planner; minggu terakhir
    /// membuka Act (MonthEnd).
    /// </summary>
    public void ContinueAfterWeek()
    {
        if (phase != GamePhase.WeekReview) return;

        if (week >= WeeksPerMonth)
        {
            SetPhase(GamePhase.MonthEnd);
            NotifyStateChanged();
            return;
        }

        week++;
        dayIndex = 0;
        SetPhase(GamePhase.Planning);
        NotifyStateChanged();
        LoadSceneIfEnabled(planningSceneName);
    }

    /// <summary>
    /// Menyelesaikan Act. Utilitas dibayar lebih dulu; kekurangannya masuk
    /// ke utang. Setelah itu pembayaran utang diterapkan.
    /// Panggil dari tombol Pay/Close month.
    /// </summary>
    public void SettleMonth(int debtPayment)
    {
        SettleMonth(debtPayment, monthlyUtilities);
    }

    /// <summary>
    /// Versi lengkap Act jika kamu ingin mengatur pembayaran utilitas sendiri.
    /// </summary>
    public void SettleMonth(int debtPayment, int utilityPayment)
    {
        if (phase != GamePhase.MonthEnd) return;

        int requestedUtilities = Mathf.Clamp(utilityPayment, 0, monthlyUtilities);
        int utilitiesPaid = Mathf.Min(requestedUtilities, gold);
        gold -= utilitiesPaid;
        monthGoldSpent += utilitiesPaid;
        monthUtilitiesPaid += utilitiesPaid;

        int unpaidUtilities = Mathf.Max(0, requestedUtilities - utilitiesPaid);
        if (unpaidUtilities > 0)
        {
            debt += unpaidUtilities;
        }

        int requestedDebt = Mathf.Max(0, debtPayment);
        int debtPaid = Mathf.Min(requestedDebt, Mathf.Min(gold, debt));
        gold -= debtPaid;
        debt -= debtPaid;
        monthGoldSpent += debtPaid;
        monthDebtPaid += debtPaid;

        bool cleared = debt <= 0;
        lastMonthReport = BuildMonthReport(cleared);

        SetPhase(cleared ? GamePhase.Ending : GamePhase.Retro);
        NotifyStateChanged();
        MonthClosed?.Invoke(lastMonthReport);
    }

    /// <summary>
    /// Dari layar Retro menuju bulan berikutnya. Utang tetap terbawa.
    /// </summary>
    public void StartNextMonth()
    {
        if (phase != GamePhase.Retro) return;

        month++;
        week = 1;
        dayIndex = 0;

        ResetMonthStats();
        SetPhase(GamePhase.Planning);
        NotifyStateChanged();
        LoadSceneIfEnabled(planningSceneName);
    }

    /// <summary>Kembali ke menu tanpa menghapus data yang sedang berjalan.</summary>
    public void ReturnToMenu()
    {
        SetPhase(GamePhase.MainMenu);
        NotifyStateChanged();
        LoadSceneIfEnabled(menuSceneName);
    }

    // ---------------------------------------------------------------------
    // Ekonomi dan statistik
    // ---------------------------------------------------------------------

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        gold += amount;
        monthGoldEarned += amount;
        NotifyStateChanged();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        monthGoldSpent += amount;
        NotifyStateChanged();
        return true;
    }

    /// <summary>Catat penjualan produk: gold masuk dan jumlah unit bertambah.</summary>
    public void RecordSale(int amount, int units = 1)
    {
        if (amount <= 0 && units <= 0) return;

        if (amount > 0)
        {
            gold += amount;
            monthGoldEarned += amount;
        }

        monthProductsSold += Mathf.Max(0, units);
        NotifyStateChanged();
    }

    /// <summary>Membayar utang lebih awal. Mengembalikan jumlah yang benar-benar dibayar.</summary>
    public int PayDebt(int amount)
    {
        int paid = Mathf.Min(Mathf.Max(0, amount), Mathf.Min(gold, debt));
        if (paid <= 0) return 0;

        gold -= paid;
        debt -= paid;
        monthGoldSpent += paid;
        monthDebtPaid += paid;
        NotifyStateChanged();
        return paid;
    }

    /// <summary>Denda, bunga, atau biaya tak terduga yang menambah utang.</summary>
    public void AddDebt(int amount)
    {
        if (amount <= 0) return;

        debt += amount;
        NotifyStateChanged();
    }

    private void PayWeeklyLivingCost()
    {
        int paid = Mathf.Min(weeklyLivingCost, gold);
        gold -= paid;
        monthGoldSpent += paid;
        monthLivingPaid += paid;

        int unpaid = weeklyLivingCost - paid;
        if (unpaid > 0)
        {
            debt += unpaid;
        }
    }

    private void ResetMonthStats()
    {
        monthStartGold = gold;
        monthStartDebt = debt;
        monthGoldEarned = 0;
        monthGoldSpent = 0;
        monthDebtPaid = 0;
        monthLivingPaid = 0;
        monthUtilitiesPaid = 0;
        monthProductsSold = 0;
        monthDaysPlayed = 0;
    }

    private MonthReport BuildMonthReport(bool debtCleared)
    {
        MonthReport report = new MonthReport
        {
            month = month,
            goldAtStart = monthStartGold,
            goldAtEnd = gold,
            debtAtStart = monthStartDebt,
            debtAtEnd = debt,
            goldEarned = monthGoldEarned,
            goldSpent = monthGoldSpent,
            debtPaid = monthDebtPaid,
            livingPaid = monthLivingPaid,
            utilitiesPaid = monthUtilitiesPaid,
            productsSold = monthProductsSold,
            daysPlayed = monthDaysPlayed,
            debtCleared = debtCleared
        };

        report.summary =
            "MONTH " + month + " CLOSED\n" +
            "Gold        " + report.goldAtStart + " -> " + report.goldAtEnd + " G\n" +
            "Debt        " + report.debtAtStart + " -> " + report.debtAtEnd + " G\n" +
            "Earned      " + report.goldEarned + " G\n" +
            "Spent       " + report.goldSpent + " G\n" +
            "Debt paid   " + report.debtPaid + " G\n" +
            "Utilities   " + report.utilitiesPaid + " G\n" +
            "Sold        " + report.productsSold + " item(s)\n" +
            "Days played " + report.daysPlayed;

        return report;
    }

    // ---------------------------------------------------------------------
    // Util
    // ---------------------------------------------------------------------

    private void SetPhase(GamePhase next)
    {
        if (phase == next) return;
        phase = next;
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private void LoadSceneIfEnabled(string sceneName)
    {
        if (!loadScenes || string.IsNullOrEmpty(sceneName)) return;
        SceneManager.LoadScene(sceneName);
    }
}
