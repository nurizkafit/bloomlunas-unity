using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menghubungkan UI kota yang sudah ada di Hierarchy (Header, DayDial,
/// PanelEventyangudahdipilih, PanelBuatMilihTempat, PanelSaatKitaKlikGedung)
/// dengan data CoreLoop dan PlannerBook.
///
/// Script ini tidak membuat panel atau tombol: semua referensi diisi lewat
/// Inspector oleh tools editor.
/// </summary>
public class CityHudUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CoreLoop coreLoop;
    [SerializeField] private PlannerBook book;

    [Header("Header")]
    [SerializeField] private TMP_Text goldValue;
    [SerializeField] private TMP_Text debtValue;
    [SerializeField] private TMP_Text calendarValue;
    [SerializeField] private TMP_Text mealpackValue;

    [Header("Panel Event (satu per hari)")]
    [SerializeField] private TMP_Text eventTime;
    [SerializeField] private TMP_Text eventTitle;

    [Header("Panel Tempat (setelah memilih)")]
    [SerializeField] private GameObject placePanel;
    [SerializeField] private TMP_Text placeTitle;
    [SerializeField] private TMP_Text placeDetail;
    [SerializeField] private Button goButton;

    [Header("Panel Pilih Tempat")]
    [SerializeField] private GameObject placeListPanel;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button marketButton;
    [SerializeField] private Button guildButton;
    [SerializeField] private Button outskirtButton;
    [SerializeField] private Button wholeTownButton;

    [Header("Tombol Bawah")]
    [SerializeField] private Button townButton;
    [SerializeField] private Button journalButton;
    [SerializeField] private Button endDayButton;

    [Header("Scene Tujuan")]
    [SerializeField] private string homeScene = "ShopdanRumah";
    [SerializeField] private string marketScene = "Market";
    [SerializeField] private string guildScene = "Guild";
    [SerializeField] private string outskirtScene = "CityOverWorld";
    [SerializeField] private string townScene = "CityOverWorld";
    [SerializeField] private string plannerScene = "MulaiGame";

    [Header("Biaya Perjalanan (menit, energi)")]
    [SerializeField] private int homeMinutes = 30;
    [SerializeField] private int marketMinutes = 30;
    [SerializeField] private int guildMinutes = 45;
    [SerializeField] private int outskirtMinutes = 60;
    [Tooltip("Energi per perpindahan tempat.")]
    [SerializeField] private int travelEnergy = 1;

    private string pendingScene;
    private string pendingName;
    private int pendingMinutes;
    private int pendingEnergy;
    private bool wired;

    private static readonly string[] EventTitles =
    {
        "A calm morning in town",
        "The bakery is baking early",
        "A courier arrives with letters",
        "Wild herbs are blooming",
        "Guild criers announce the day",
        "Travellers browse the stalls",
        "Evening lanterns are lit"
    };

    private void Awake()
    {
        Resolve();
        Wire();
        if (placePanel != null) placePanel.SetActive(false);
    }

    private void OnEnable()
    {
        Resolve();
        Wire();
        if (coreLoop != null) coreLoop.StateChanged += Refresh;
        if (book != null) book.PlannerChanged += Refresh;
        if (placePanel != null) placePanel.SetActive(false);
        Refresh();
    }

    private void OnDisable()
    {
        if (coreLoop != null) coreLoop.StateChanged -= Refresh;
        if (book != null) book.PlannerChanged -= Refresh;
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

        Wire(homeButton, () => ShowPlace("Home and Shop", homeScene, homeMinutes,
            "Open Mon-Sat 06:00-17:00."));
        Wire(marketButton, () => ShowPlace("Market", marketScene, marketMinutes,
            "Stalls open 07:00-17:00."));
        Wire(guildButton, () => ShowPlace("Guild Hall", guildScene, guildMinutes,
            "Guild desks and quest board, 08:00-16:00."));
        Wire(outskirtButton, () => ShowPlace("City Outskirt", outskirtScene, outskirtMinutes,
            "Gathering area, best before dusk."));
        Wire(wholeTownButton, () => ShowPlace("See the whole town", townScene, 0,
            "Back to the town view.", 0));
        Wire(townButton, TogglePlaceList);
        Wire(journalButton, OpenPlanner);
        Wire(endDayButton, EndDay);
        Wire(goButton, Go);
    }

    private void Wire(Button button, System.Action action)
    {
        if (button == null || action == null) return;
        button.onClick.AddListener(() => action());
    }

    /// <summary>Dipanggil dari klik gedung 3D (CityCameraController).</summary>
    public void SelectLocation(CityLocation location)
    {
        if (location == null) return;

        string scene = string.IsNullOrEmpty(location.TargetScene) ? location.DisplayName : location.TargetScene;
        ShowPlace(location.DisplayName, scene, marketMinutes, "Tap Go to enter this place.");
    }

    public void ShowPlace(string displayName, string sceneName, int minutes, string detail,
        int energyCost = -1)
    {
        pendingName = displayName;
        pendingScene = sceneName;
        pendingMinutes = Mathf.Max(0, minutes);
        pendingEnergy = energyCost >= 0 ? energyCost : travelEnergy;

        if (placeTitle != null) placeTitle.text = displayName;
        if (placeDetail != null)
        {
            string arrive = coreLoop != null ? ArrivalText(pendingMinutes) : "";
            placeDetail.text = detail + "\n" + pendingMinutes + "m   -" + pendingEnergy +
                               " energy   " + (string.IsNullOrEmpty(arrive) ? "" : "arrive " + arrive);
        }

        if (placePanel != null) placePanel.SetActive(true);
    }

    private void Go()
    {
        if (string.IsNullOrEmpty(pendingScene)) return;

        if (coreLoop != null && !coreLoop.SpendTime(pendingMinutes, pendingEnergy))
        {
            if (placeDetail != null) placeDetail.text = "Not enough energy to travel. Rest first.";
            return;
        }

        if (placePanel != null) placePanel.SetActive(false);

        if (SceneManager.GetActiveScene().name != pendingScene)
        {
            SceneManager.LoadScene(pendingScene);
        }
        else
        {
            Refresh();
        }
    }

    private void TogglePlaceList()
    {
        if (placeListPanel == null) return;
        placeListPanel.SetActive(!placeListPanel.activeSelf);
    }

    /// <summary>
    /// Menutup hari: CoreLoop maju ke hari berikutnya, jam dan energi direset,
    /// lalu scene dimuat ulang supaya panel event mengikuti rencana hari baru.
    /// </summary>
    private void EndDay()
    {
        if (coreLoop == null) return;

        coreLoop.CompleteDay();

        if (coreLoop.Phase == GamePhase.DayReview)
        {
            coreLoop.ContinueAfterReview();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (coreLoop.Phase == GamePhase.WeekReview)
        {
            coreLoop.ContinueAfterWeek();
            if (coreLoop.Phase == GamePhase.Planning) SceneManager.LoadScene(plannerScene);
        }
    }

    private void OpenPlanner()
    {
        if (string.IsNullOrEmpty(plannerScene)) return;
        if (SceneManager.GetActiveScene().name == plannerScene) return;
        SceneManager.LoadScene(plannerScene);
    }

    private void Refresh()
    {
        Resolve();

        int gold = coreLoop != null ? coreLoop.Gold : 0;
        int debt = coreLoop != null ? coreLoop.Debt : 0;
        int month = coreLoop != null ? coreLoop.Month : 1;
        int week = coreLoop != null ? coreLoop.Week : 1;
        int day = coreLoop != null ? coreLoop.DayIndex + 1 : 1;

        if (goldValue != null) goldValue.text = gold + " G";
        if (debtValue != null) debtValue.text = debt + " G";
        if (calendarValue != null) calendarValue.text = "M" + month + " W" + week + " D" + day;
        if (mealpackValue != null) mealpackValue.text = (coreLoop != null ? coreLoop.Energy : 0).ToString();

        ShowTodayEvent();
    }

    /// <summary>Panel event hanya menampilkan satu kejadian sesuai goal hari ini.</summary>
    private void ShowTodayEvent()
    {
        if (eventTitle == null && eventTime == null) return;

        string title = "";
        if (book != null && coreLoop != null)
        {
            PlannerDayPlan plan = book.GetDayPlan(coreLoop.DayIndex);
            if (plan != null && plan.hasGoal && plan.goal != null)
            {
                title = TextFor(plan.goal.goalType);
            }
        }

        if (string.IsNullOrEmpty(title))
        {
            int index = coreLoop != null ? coreLoop.DayIndex % EventTitles.Length : 0;
            title = EventTitles[index];
        }

        if (eventTitle != null) eventTitle.text = title;
        if (eventTime != null)
        {
            string time = coreLoop != null ? coreLoop.ClockText : "06:00";
            eventTime.text = "EVENT  " + time;
        }
    }

    private static string TextFor(PlannerGoalType type)
    {
        switch (type)
        {
            case PlannerGoalType.Explore: return "Wild herbs are blooming outside town";
            case PlannerGoalType.Craft: return "The workbench is warm and ready";
            case PlannerGoalType.OpenShop: return "The shop bell rings all morning";
            case PlannerGoalType.MarketGuild: return "Guild criers announce new prices";
            case PlannerGoalType.FreeTime: return "A quiet afternoon at home";
            default: return "A calm day in Bloom Lunas";
        }
    }

    private string ArrivalText(int minutes)
    {
        if (coreLoop == null) return "";
        int total = Mathf.Clamp(coreLoop.TimeMinutes + Mathf.Max(0, minutes), 0, 23 * 60 + 59);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}
