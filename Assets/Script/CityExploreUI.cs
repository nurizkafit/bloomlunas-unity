using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gameplay City Outskirt (tidak pindah scene). Semua panel dan baris sudah
/// ada di scene; script ini hanya menyalakan/mematikan dan mengisi datanya.
///
/// Alur: pilih City Outskirt -> Go -> tiba -> PanelSebelumConfirmexplore ->
/// Explore the Outskirt -> PanelCityOutskirt -> Keep walking -> You found a
/// spot -> Gather / Pass by. Town atau Go home kembali ke peta kota.
/// </summary>
public class CityExploreUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CoreLoop coreLoop;
    [SerializeField] private PlannerBook book;
    [SerializeField] private CityHudUI hud;
    [SerializeField] private CityCameraController cameraRig;

    [Header("Panel")]
    [SerializeField] private GameObject hubEventPanel;
    [SerializeField] private GameObject hubPlaceListPanel;
    [SerializeField] private GameObject confirmPanel;
    [Tooltip("Panel berisi 3 tombol (Explore / Town / Go home) di dalam confirmPanel.")]
    [SerializeField] private GameObject confirmButtonsPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject foundPanel;

    [Header("Tombol")]
    [SerializeField] private Button exploreButton;
    [SerializeField] private Button keepWalkingButton;
    [SerializeField] private Button headBackButton;
    [SerializeField] private Button townButton;
    [SerializeField] private Button goHomeButton;
    [SerializeField] private Button gatherButton;
    [SerializeField] private Button passByButton;

    [Header("Daftar Spot & Keranjang")]
    [SerializeField] private GatherSpotRow[] spots;
    [SerializeField] private BasketRowView[] basketRows;
    [SerializeField] private TMP_Text basketCountLabel;

    [Header("Panel You Found a Spot")]
    [SerializeField] private TMP_Text foundName;
    [SerializeField] private TMP_Text foundTime;
    [SerializeField] private TMP_Text foundAmount;

    [Header("Aturan")]
    [SerializeField, Min(1)] private int basketCapacity = 5;
    [SerializeField, Min(0)] private int walkMinutes = 45;
    [SerializeField, Min(0)] private int walkEnergy = 1;
    [SerializeField, Min(0)] private int gatherEnergy = 1;

    private readonly Dictionary<string, int> basket = new Dictionary<string, int>();
    private GatherSpotRow currentSpot;
    private bool wired;

    public bool IsExploring => confirmPanel != null && confirmPanel.activeSelf;

    private void Awake()
    {
        Resolve();
        Wire();
        HideAll();
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
        if (hud == null) hud = FindObjectOfType<CityHudUI>(true);
        if (cameraRig == null) cameraRig = FindObjectOfType<CityCameraController>(true);
    }

    private void Wire()
    {
        if (wired) return;
        wired = true;

        Wire(exploreButton, ExploreOutskirt);
        Wire(keepWalkingButton, KeepWalking);
        Wire(headBackButton, HeadBack);
        Wire(townButton, Leave);
        Wire(goHomeButton, Leave);
        Wire(gatherButton, Gather);
        Wire(passByButton, PassBy);
    }

    private void Wire(Button button, System.Action action)
    {
        if (button == null || action == null) return;
        button.onClick.AddListener(() => action());
    }

    private void HideAll()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (foundPanel != null) foundPanel.SetActive(false);
    }

    /// <summary>Dipanggil CityHudUI setelah pemain tiba di City Outskirt.</summary>
    public void Enter()
    {
        Resolve();

        // Stamina habis tetap boleh berjalan; hanya aktivitas yang diblokir.
        if (hubEventPanel != null) hubEventPanel.SetActive(false);
        if (hubPlaceListPanel != null) hubPlaceListPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(true);
        if (confirmButtonsPanel != null) confirmButtonsPanel.SetActive(true);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (foundPanel != null) foundPanel.SetActive(false);
        // Keep walking / Head back hanya dipakai saat sudah di dalam outskirt.
        if (keepWalkingButton != null) keepWalkingButton.gameObject.SetActive(false);
        if (headBackButton != null) headBackButton.gameObject.SetActive(false);

        basket.Clear();
        currentSpot = null;
        if (basketCountLabel != null) basketCountLabel.text = "0/" + basketCapacity;
        RefreshBasket();
    }

    /// <summary>
    /// panelPanel tetap aktif karena PanelCityOutskirt adalah child-nya;
    /// yang disembunyikan hanya deretan tombol Explore/Town/Go home.
    /// </summary>
    private void ExploreOutskirt()
    {
        if (confirmButtonsPanel != null) confirmButtonsPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
        if (foundPanel != null) foundPanel.SetActive(false);
        if (keepWalkingButton != null) keepWalkingButton.gameObject.SetActive(true);
        if (headBackButton != null) headBackButton.gameObject.SetActive(true);
    }

    private void KeepWalking()
    {
        if (confirmButtonsPanel != null) confirmButtonsPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
        if (keepWalkingButton != null) keepWalkingButton.gameObject.SetActive(true);
        if (headBackButton != null) headBackButton.gameObject.SetActive(true);

        Spend(walkMinutes, walkEnergy);

        currentSpot = PickSpot();
        if (currentSpot == null)
        {
            ShowMessage("No more spots to search today.");
            return;
        }

        if (foundName != null) foundName.text = "You found " + currentSpot.ItemName;
        if (foundTime != null) foundTime.text = currentSpot.TimeText + "   -" + gatherEnergy + " energy";
        if (foundAmount != null) foundAmount.text = "Up to 2";
        if (foundPanel != null) foundPanel.SetActive(true);
        if (gatherButton != null) gatherButton.interactable = coreLoop == null || coreLoop.Energy > 0;

        if (cameraRig != null) cameraRig.FocusOnPoint(PlayerPoint(), 0.55f);
    }

    private void Gather()
    {
        if (currentSpot == null) return;

        if (coreLoop != null && coreLoop.Energy <= 0)
        {
            ShowMessage("Too tired to gather.\nGo home, rest or eat first.");
            return;
        }

        if (BasketSlots >= basketCapacity)
        {
            ShowMessage("Your basket is full.\nGo home to unload.");
            return;
        }

        if (book != null)
        {
            book.AddInventory(currentSpot.ItemId, currentSpot.ItemName, currentSpot.Amount, false);
        }

        int amount;
        basket.TryGetValue(currentSpot.ItemId, out amount);
        basket[currentSpot.ItemId] = amount + currentSpot.Amount;

        Spend(currentSpot.Minutes, gatherEnergy);
        if (currentSpot != null) currentSpot.gameObject.SetActive(false);
        currentSpot = null;
        if (foundPanel != null) foundPanel.SetActive(false);

        RefreshBasket();

        if (book != null && coreLoop != null)
        {
            PlannerDayPlan plan = book.GetDayPlan(coreLoop.DayIndex);
            if (plan != null && plan.hasGoal && plan.goal != null &&
                plan.goal.goalType == PlannerGoalType.Explore)
            {
                book.CompleteCurrentGoal();
            }
        }
    }

    private void PassBy()
    {
        currentSpot = null;
        if (foundPanel != null) foundPanel.SetActive(false);
        if (foundName != null) foundName.text = "You found a spot";
        if (foundTime != null) foundTime.text = string.Empty;
        if (foundAmount != null) foundAmount.text = string.Empty;
    }

    private void HeadBack()
    {
        if (foundPanel != null) foundPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (confirmButtonsPanel != null) confirmButtonsPanel.SetActive(true);
        if (keepWalkingButton != null) keepWalkingButton.gameObject.SetActive(false);
        if (headBackButton != null) headBackButton.gameObject.SetActive(false);
    }

    private void Leave()
    {
        HideAll();
        if (hud != null) hud.ReturnToHub();
    }

    private void Spend(int minutes, int energyCost)
    {
        if (coreLoop == null) return;
        int cost = Mathf.Min(Mathf.Max(0, energyCost), coreLoop.Energy);
        coreLoop.SpendTime(minutes, cost);
    }

    private GatherSpotRow PickSpot()
    {
        if (spots == null || spots.Length == 0) return null;

        List<GatherSpotRow> available = new List<GatherSpotRow>();
        for (int i = 0; i < spots.Length; i++)
        {
            if (spots[i] != null && spots[i].gameObject.activeSelf) available.Add(spots[i]);
        }

        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    private int BasketSlots
    {
        get
        {
            int slots = 0;
            foreach (KeyValuePair<string, int> pair in basket)
            {
                if (pair.Value > 0) slots++;
            }
            return slots;
        }
    }

    private void RefreshBasket()
    {
        if (basketRows == null) return;

        for (int i = 0; i < basketRows.Length; i++)
        {
            BasketRowView row = basketRows[i];
            if (row == null) continue;

            int amount;
            basket.TryGetValue(row.ItemId, out amount);
            row.Show(amount);
        }

        if (basketCountLabel != null)
        {
            basketCountLabel.text = BasketSlots + "/" + basketCapacity;
        }
    }

    private void ShowMessage(string message)
    {
        if (foundPanel != null) foundPanel.SetActive(true);
        if (foundName != null) foundName.text = message;
        if (foundTime != null) foundTime.text = string.Empty;
        if (foundAmount != null) foundAmount.text = string.Empty;
        if (gatherButton != null) gatherButton.interactable = false;
    }

    private Vector3 PlayerPoint()
    {
        CityPlayerController player = FindObjectOfType<CityPlayerController>(true);
        return player != null ? player.transform.position : Vector3.zero;
    }
}
