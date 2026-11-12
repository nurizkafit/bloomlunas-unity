using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD opsional untuk CityOverWorld. Tempel di Canvas kota dan isi teksnya
/// lewat Inspector. Rencana dibaca dari PlannerBook, jadi tetap sama setelah
/// pindah scene.
/// </summary>
public class PlannerCityHUD : MonoBehaviour
{
    [SerializeField] private PlannerBook book;
    [SerializeField] private CoreLoop coreLoop;

    [Header("Teks Kota")]
    [SerializeField] private TMP_Text weekLabel;
    [SerializeField] private TMP_Text dayLabel;
    [SerializeField] private TMP_Text goalLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text debtLabel;

    [Header("Papan Minggu (opsional, isi 7)")]
    [SerializeField] private TMP_Text[] dayGoalLabels = new TMP_Text[7];

    [Header("Tombol Goal")]
    [SerializeField] private Button completeGoalButton;

    private void Awake()
    {
        Resolve();
        if (completeGoalButton != null) completeGoalButton.onClick.AddListener(CompleteGoal);
    }

    private void OnEnable()
    {
        Resolve();
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
        if (book == null) book = PlannerBook.Instance;
        if (coreLoop == null) coreLoop = CoreLoop.Instance;
    }

    public void CompleteGoal()
    {
        if (book != null) book.CompleteCurrentGoal();
        Refresh();
    }

    public void Refresh()
    {
        Resolve();
        if (book == null || coreLoop == null) return;

        int day = Mathf.Clamp(coreLoop.DayIndex, 0, 6);
        PlannerDayPlan plan = book.GetDayPlan(day);
        PlannerGoalCardData goal = plan != null && plan.hasGoal ? plan.goal : null;

        if (weekLabel != null)
        {
            weekLabel.text = "MONTH " + coreLoop.Month + "  |  WEEK " + coreLoop.Week;
        }

        if (dayLabel != null)
        {
            dayLabel.text = coreLoop.DayName + "  |  D" + (day + 1);
        }

        if (goalLabel != null)
        {
            goalLabel.text = goal == null
                ? "No goal planned for today."
                : (plan.completed ? "Done: " : "Today: ") + goal.activityName;
        }

        if (statusLabel != null)
        {
            if (goal == null) statusLabel.text = "Open the planner and drop a card on this day.";
            else if (plan.completed) statusLabel.text = "Goal complete. End the day when ready.";
            else statusLabel.text = goal.subtext + "  -  " + goal.locationName;
        }

        if (debtLabel != null)
        {
            debtLabel.text = "Gold " + coreLoop.Gold + "  |  Debt " + coreLoop.Debt;
        }

        if (completeGoalButton != null)
        {
            completeGoalButton.interactable = goal != null && !plan.completed;
        }

        RefreshWeekBoard();
    }

    private void RefreshWeekBoard()
    {
        if (dayGoalLabels == null) return;

        for (int i = 0; i < dayGoalLabels.Length && i < 7; i++)
        {
            if (dayGoalLabels[i] == null) continue;
            PlannerDayPlan day = book.GetDayPlan(i);
            string name = CoreLoop.DayNames[i].Substring(0, 3).ToUpperInvariant();
            if (day == null || !day.hasGoal)
            {
                dayGoalLabels[i].text = name + "  -";
            }
            else
            {
                string mark = day.completed ? "  [x]" : day.missed ? "  [!]" : string.Empty;
                dayGoalLabels[i].text = name + "  " + day.goal.activityName + mark;
            }
        }
    }
}
