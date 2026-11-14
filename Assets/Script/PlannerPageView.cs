using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Menampilkan isi satu halaman planner ke TMP_Text yang sudah ada di scene.
/// Teks memakai link TMP untuk tombol kecil seperti [-], [+], L/F/H, dan
/// Deliver, sehingga tidak perlu membuat panel baru saat Play.
/// </summary>
public class PlannerPageView : MonoBehaviour, IPointerClickHandler
{
    private const string Accent = "#2F5E4E";
    private const string Muted = "#7A6A50";
    private const string Gold = "#A77A1E";
    private const string Red = "#A33B2E";
    private const string Green = "#2F6B4F";
    private const string Ink = "#2B241B";

    [SerializeField] private PlannerPageId pageId;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text rightText;
    [SerializeField] private TMP_Text footerText;
    [SerializeField] private TMP_Text goalCountLabel;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private bool autoResolveTexts = true;

    private PlannerBook book;
    private bool subscribed;

    private void Awake()
    {
        ResolveBook();
        ResolveTexts();
        StyleTexts();
    }

    private void OnEnable()
    {
        ResolveBook();
        ResolveTexts();
        Subscribe();
        if (autoRefresh) Refresh();
    }

    private void OnDisable()
    {
        if (book != null && subscribed)
        {
            book.PlannerChanged -= Refresh;
            subscribed = false;
        }
    }

    private void Start()
    {
        ResolveBook();
        ResolveTexts();
        Subscribe();
        if (autoRefresh) Refresh();
    }

    private void ResolveBook()
    {
        if (book == null) book = PlannerBook.Instance;
    }

    private void ResolveTexts()
    {
        if (!autoResolveTexts) return;

        if (titleText == null) titleText = FindText("Page/Title");
        if (subtitleText == null) subtitleText = FindText("Page/Subtitle");
        if (bodyText == null) bodyText = FindText("Page/Body");
        if (footerText == null) footerText = FindText("Page/Footer");

        // Halaman Goals memakai buku dua halaman, bukan Page tunggal.
        if (titleText == null) titleText = FindText("BookCover/Pages/LeftPage/PageTitle");
        if (goalCountLabel == null) goalCountLabel = FindText("BookCover/Pages/LeftPage/GoalCount");

        // Quest memakai dua halaman tanpa Page/Body; label isi sudah diganti
        // menjadi Body di scene supaya tetap memakai TMP yang ada.
        if (titleText == null) titleText = FindText("Pages/LeftPage/PageTitle");
        if (bodyText == null) bodyText = FindText("Pages/LeftPage/Body");
        if (rightText == null) rightText = FindText("Pages/RightPage/Body");
    }

    private TMP_Text FindText(string path)
    {
        Transform found = transform.Find(path);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private void StyleTexts()
    {
        if (bodyText != null)
        {
            bodyText.richText = true;
            bodyText.raycastTarget = true;
            bodyText.fontSize = Mathf.Clamp(bodyText.fontSize, 18f, 22f);
            bodyText.lineSpacing = 6f;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Overflow;
            bodyText.color = Hex(Ink);
        }

        if (rightText != null)
        {
            rightText.richText = true;
            rightText.raycastTarget = true;
            rightText.fontSize = Mathf.Clamp(rightText.fontSize, 17f, 21f);
            rightText.lineSpacing = 6f;
            rightText.color = Hex(Ink);
        }

        if (titleText != null) titleText.fontSize = Mathf.Max(36f, titleText.fontSize);
        if (subtitleText != null) subtitleText.fontSize = Mathf.Max(18f, subtitleText.fontSize);
        if (footerText != null) footerText.fontSize = Mathf.Max(16f, footerText.fontSize);
        if (goalCountLabel != null) goalCountLabel.fontSize = Mathf.Max(20f, goalCountLabel.fontSize);
    }

    private void Subscribe()
    {
        if (book != null && !subscribed)
        {
            book.PlannerChanged += Refresh;
            subscribed = true;
        }
    }

    public void Refresh()
    {
        book = book != null ? book : PlannerBook.Instance;
        if (book == null) return;

        switch (pageId)
        {
            case PlannerPageId.Store: RefreshStore(); break;
            case PlannerPageId.Ledger: RefreshLedger(); break;
            case PlannerPageId.Market: RefreshMarket(); break;
            case PlannerPageId.Hours: RefreshHours(); break;
            case PlannerPageId.Skills: RefreshSkills(); break;
            case PlannerPageId.Notes: RefreshNotes(); break;
            case PlannerPageId.Quest: RefreshQuest(); break;
            default: RefreshGoals(); break;
        }
    }

    private void RefreshGoals()
    {
        SetTitle("Week " + Week, "Plan one goal for each day.");
        if (goalCountLabel != null) goalCountLabel.text = book.PlannedDayCount + "/7";
        SetFooter(book.MissingDayCount > 0
            ? book.MissingDayCount + " day(s) still need a goal."
            : "All seven days are planned.");
    }

    private void RefreshStore()
    {
        SetTitle("Products to sell", "Set units and split your gold into the three jars.");

        StringBuilder body = new StringBuilder();
        body.AppendLine("<size=22><b>PRODUCTS</b></size>");
        for (int i = 0; i < book.Products.Count; i++)
        {
            PlannerProductOffer product = book.Products[i];
            body.AppendLine(
                "<b>" + product.displayName + "</b>  " +
                "<color=" + Muted + ">demand " + product.demandMin + "-" + product.demandMax + "</color>  " +
                "<color=" + Gold + ">price " + book.GetPrice(product.productId).ToString("0.0") + "</color>  " +
                "units <b>" + product.units + "</b>  " +
                Link("product:" + product.productId + ":-1", "[-]", Red) + " " +
                Link("product:" + product.productId + ":1", "[+]", Green));
        }

        if (book.TotalProductUnits == 0) body.AppendLine("<color=" + Muted + ">No product selected yet.</color>");

        body.AppendLine();
        body.AppendLine("<size=22><b>BUDGET JARS</b></size>");
        AppendJar(body, "Store", PlannerJarKind.Store, book.Budget.store);
        AppendJar(body, "Personal", PlannerJarKind.Personal, book.Budget.personal);
        AppendJar(body, "Buffer", PlannerJarKind.Buffer, book.Budget.buffer);

        body.AppendLine();
        body.AppendLine("<color=" + Muted + ">In jars</color> <b>" + book.JarTotal + " / " + book.Gold + "</b>");
        body.AppendLine("<color=" + Muted + ">Living cost</color> <b><color=" + Red + ">-" + book.WeeklyLivingCost + "</color></b>");
        body.AppendLine("<color=" + Muted + ">Gold after plan</color> <b>" + book.GoldAfterPlan + "</b>");

        body.AppendLine();
        body.AppendLine("<size=22><b>HERBS YOU NEED</b></size>");
        bool anyNeed = false;
        for (int i = 0; i < book.Products.Count; i++)
        {
            PlannerProductOffer product = book.Products[i];
            if (product.units <= 0) continue;
            anyNeed = true;
            for (int j = 0; j < product.ingredients.Count; j++)
            {
                PlannerIngredientNeed need = product.ingredients[j];
                int have = book.GetAmount(need.itemId);
                bool ok = have >= need.needed;
                body.AppendLine(
                    need.displayName + "  " +
                    "<color=" + (ok ? Green : Red) + ">" + have + "/" + need.needed +
                    (ok ? " OK" : " MISSING") + "</color>");
            }
        }

        if (!anyNeed) body.AppendLine("<color=" + Muted + ">Choose products first.</color>");

        SetBody(body.ToString());
        SetFooter("The shelf stock is different from products in storage.");
    }

    private void AppendJar(StringBuilder body, string label, PlannerJarKind kind, int amount)
    {
        body.AppendLine(
            "<b>" + label + "</b>  " + amount + "  " +
            Link("jar:" + kind + ":-5", "[-]", Red) + " " +
            Link("jar:" + kind + ":5", "[+]", Green));
    }

    private void RefreshLedger()
    {
        SetTitle("Ledger", "Every coin that comes in and goes out this week.");

        StringBuilder body = new StringBuilder();
        body.AppendLine("<size=22><b>THIS WEEK</b></size>");
        body.AppendLine("Earned  <b><color=" + Green + ">+" + book.WeekEarned + "</color></b>");
        body.AppendLine("Spent   <b><color=" + Red + ">-" + book.WeekSpent + "</color></b>");
        body.AppendLine();
        body.AppendLine("<size=22><b>JARS SPENT</b></size>");
        body.AppendLine("Store     " + book.JarSpent(PlannerJarKind.Store) + " / " + book.Budget.store);
        body.AppendLine("Personal  " + book.JarSpent(PlannerJarKind.Personal) + " / " + book.Budget.personal);
        body.AppendLine("Buffer    " + book.JarSpent(PlannerJarKind.Buffer) + " / " + book.Budget.buffer);
        body.AppendLine();
        body.AppendLine("<size=22><b>LATEST ENTRIES</b></size>");

        if (book.Ledger.Count == 0)
        {
            body.AppendLine("<color=" + Muted + ">No entry yet.</color>");
        }
        else
        {
            int start = Mathf.Max(0, book.Ledger.Count - 10);
            for (int i = start; i < book.Ledger.Count; i++)
            {
                PlannerLedgerEntry entry = book.Ledger[i];
                string sign = entry.kind == PlannerLedgerKind.Earn ? "+" :
                    entry.kind == PlannerLedgerKind.Spend ? "-" : " ";
                string color = entry.kind == PlannerLedgerKind.Earn ? Green :
                    entry.kind == PlannerLedgerKind.Spend ? Red : Muted;
                body.AppendLine(
                    "<color=" + Muted + ">" + entry.weekLabel + "</color>  " +
                    "<color=" + color + ">" + sign + entry.amount + "</color>  " +
                    entry.description);
            }
        }

        body.AppendLine();
        body.AppendLine("<size=22><b>WHAT YOU OWN</b></size>");
        body.AppendLine("Gold  <b>" + book.Gold + "</b>");
        body.AppendLine("Debt  <b><color=" + Red + ">" + book.Debt + "</color></b>");

        body.AppendLine();
        body.AppendLine("<b>Products</b>");
        for (int i = 0; i < book.Inventory.Count; i++)
        {
            if (book.Inventory[i].isProduct)
            {
                body.AppendLine("  " + book.Inventory[i].displayName + "  x" + book.Inventory[i].amount);
            }
        }

        body.AppendLine("<b>Materials</b>");
        for (int i = 0; i < book.Inventory.Count; i++)
        {
            if (!book.Inventory[i].isProduct)
            {
                body.AppendLine("  " + book.Inventory[i].displayName + "  x" + book.Inventory[i].amount);
            }
        }

        SetBody(body.ToString());
        SetFooter("Gold and debt follow CoreLoop. Sales update both.");
    }

    private void RefreshMarket()
    {
        SetTitle("Market", "Prices move. Choose a shelf price before you sell.");

        StringBuilder body = new StringBuilder();
        body.AppendLine("<size=22><b>PRODUCTS</b></size>");
        for (int i = 0; i < book.MarketItems.Count; i++)
        {
            PlannerMarketItem item = book.MarketItems[i];
            if (!item.isProduct) continue;
            body.AppendLine(
                "<b>" + item.displayName + "</b>  " +
                "<color=" + Muted + ">demand " + item.demandMin.ToString("0.#") + "-" +
                item.demandMax.ToString("0.#") + "</color>  " +
                "usual " + item.usualPrice.ToString("0.0") + "  " +
                "now <color=" + Gold + ">" + book.GetPrice(item.itemId).ToString("0.0") + "</color>  " +
                TrendText(item.trend) + "  " +
                Link("band:" + item.itemId + ":0", "L", Red) + " " +
                Link("band:" + item.itemId + ":1", "F", Muted) + " " +
                Link("band:" + item.itemId + ":2", "H", Green));
        }

        body.AppendLine();
        body.AppendLine("<size=22><b>MATERIALS</b></size>");
        for (int i = 0; i < book.MarketItems.Count; i++)
        {
            PlannerMarketItem item = book.MarketItems[i];
            if (item.isProduct) continue;
            body.AppendLine(
                item.displayName + "  " +
                "<color=" + Muted + ">demand " + item.demandMin.ToString("0.#") + "-" +
                item.demandMax.ToString("0.#") + "</color>  " +
                "now <color=" + Gold + ">" + book.GetPrice(item.itemId).ToString("0.0") + "</color>  " +
                TrendText(item.trend));
        }

        body.AppendLine();
        body.AppendLine("<color=" + Muted + ">L = low price, F = fair price, H = high price.</color>");
        SetBody(body.ToString());
        SetFooter("Low sells faster. High earns more per unit but fewer customers.");
    }

    private void RefreshHours()
    {
        SetTitle("Opening hours", "Come early. The walk time is added before you arrive.");

        StringBuilder body = new StringBuilder();
        for (int i = 0; i < book.Schedule.Count; i++)
        {
            PlannerScheduleEntry entry = book.Schedule[i];
            string hours = entry.openHour == 0 && entry.closeHour >= 24
                ? "24 hours"
                : entry.openHour + ":00 - " + entry.closeHour + ":00";
            string closed = entry.closedDay >= 0
                ? "closed " + CoreLoop.DayNames[entry.closedDay]
                : "open every day";
            body.AppendLine("<b>" + entry.locationName + "</b>  " + hours);
            body.AppendLine("  <color=" + Muted + ">" + closed + "  |  walk " + entry.walkMinutes + " min</color>");
        }

        body.AppendLine();
        body.AppendLine("<b>Every week</b>");
        body.AppendLine("  New commissions at the guild  <color=" + Muted + ">Monday</color>");
        body.AppendLine("  Living costs paid at week end  <color=" + Red + ">-" + book.WeeklyLivingCost + "</color>");
        body.AppendLine();
        body.AppendLine("<b>Month end</b>");
        body.AppendLine("  Debt payment, minimum  <color=" + Red + ">" + book.MinimumDebtPayment + "</color>");
        body.AppendLine("  Utilities  <color=" + Red + ">" + book.MonthlyUtilities + "</color>");

        SetBody(body.ToString());
        SetFooter("Market closed Friday. Guild Hall closed Sunday.");
    }

    private void RefreshSkills()
    {
        SetTitle("Skills", "Each daily goal you meet gives " + book.SkillXpPerGoal + " XP.");

        StringBuilder body = new StringBuilder();
        AddSkill(body, PlannerSkillBranch.Moon, "Moon", "Crafting and processing.");
        AddSkill(body, PlannerSkillBranch.Sun, "Sun", "Selling and trade.");
        AddSkill(body, PlannerSkillBranch.Earth, "Earth", "Gathering.");
        body.AppendLine("<color=" + Muted + ">Moon raises product quality. Sun adds customers. Earth adds herbs.</color>");

        SetBody(body.ToString());
        SetFooter("Skill XP is saved in PlannerBook and survives scene changes.");
    }

    private void AddSkill(StringBuilder body, PlannerSkillBranch branch, string name, string description)
    {
        int level = book.GetSkillLevel(branch);
        int xp = book.GetSkillXp(branch);
        body.AppendLine("<b>" + name + "</b>  Lv " + level + "  <color=" + Muted + ">" + description + "</color>");
        body.AppendLine("  <color=" + Accent + ">" + xp + "/" + book.SkillXpForNextLevel + " XP</color>");
        body.AppendLine();
    }

    private void RefreshNotes()
    {
        SetTitle("Notes to self", "What you learned last week.");

        StringBuilder body = new StringBuilder();
        body.AppendLine("<size=22><b>FROM LAST WEEK</b></size>");
        body.AppendLine(string.IsNullOrEmpty(book.LastWeekNote)
            ? "<color=" + Muted + ">You left no note last week.</color>"
            : book.LastWeekNote);

        body.AppendLine();
        body.AppendLine("<size=22><b>FIRST STEPS  " + book.CompletedFirstSteps + "/" +
                        book.FirstSteps.Count + "</b></size>");
        for (int i = 0; i < book.FirstSteps.Count; i++)
        {
            PlannerFirstStep step = book.FirstSteps[i];
            string mark = step.done ? "[x]" : "[ ]";
            body.AppendLine(Link("step:" + i, mark, step.done ? Green : Muted) + "  " + step.text);
        }

        SetBody(body.ToString());
        SetFooter("Click a box to mark a first step.");
    }

    private void RefreshQuest()
    {
        SetTitle("Taken Commission", "Take a commission at the Guild quest board.");

        StringBuilder left = new StringBuilder();
        left.AppendLine("<size=22><b>COMMISSIONS</b></size>");
        if (book.Commissions.Count == 0)
        {
            left.AppendLine("<color=" + Muted + ">You have no commission. Take one at the Guild quest board.</color>");
        }
        else
        {
            for (int i = 0; i < book.Commissions.Count; i++)
            {
                PlannerCommission commission = book.Commissions[i];
                int have = book.GetAmount(commission.itemId);
                bool ready = have >= commission.required;
                left.AppendLine(
                    "<b>" + commission.title + "</b>  " +
                    "<color=" + (ready ? Green : Muted) + ">" + have + "/" + commission.required + "</color>  " +
                    "<color=" + Gold + ">+" + commission.reward + "</color>  " +
                    "[" + commission.status + "]  " +
                    (ready ? Link("deliver:" + commission.commissionId, "Deliver", Green) : string.Empty));
            }
        }

        StringBuilder right = new StringBuilder();
        right.AppendLine("<size=22><b>YOUR REQUESTS</b></size>");
        if (book.Requests.Count == 0)
        {
            right.AppendLine("<color=" + Muted + ">You have no open request. Post one at the Guild quest board.</color>");
        }
        else
        {
            for (int i = 0; i < book.Requests.Count; i++)
            {
                PlannerRequest request = book.Requests[i];
                right.AppendLine(
                    request.itemId + "  x" + request.quantity +
                    "  <color=" + Muted + ">arrives D" + (request.arrivalDay + 1) + "</color>  " +
                    "[" + request.status + "]");
            }
        }

        SetBody(left.ToString());
        SetRight(right.ToString());
        SetFooter("A request arrives in your stock in the morning of its day.");
    }

    private void SetTitle(string title, string subtitle)
    {
        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = subtitle;
    }

    private void SetBody(string value)
    {
        if (bodyText != null) bodyText.text = value;
    }

    private void SetRight(string value)
    {
        if (rightText != null) rightText.text = value;
    }

    private void SetFooter(string value)
    {
        if (footerText != null) footerText.text = value;
    }

    private int Week => book != null && book.CurrentWeek != null ? book.CurrentWeek.week :
        (CoreLoop.Instance != null ? CoreLoop.Instance.Week : 1);

    private static string TrendText(PlannerMarketTrend trend)
    {
        switch (trend)
        {
            case PlannerMarketTrend.Up: return "<color=" + Green + ">^</color>";
            case PlannerMarketTrend.Down: return "<color=" + Red + ">v</color>";
            default: return "<color=" + Muted + ">-</color>";
        }
    }

    private static string Link(string id, string text, string color)
    {
        return "<link=\"" + id + "\"><color=" + color + "><b>" + text + "</b></color></link>";
    }

    private static Color Hex(string value)
    {
        Color color;
        return ColorUtility.TryParseHtmlString(value, out color) ? color : Color.black;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (book == null) ResolveBook();

        if (TryLink(bodyText, eventData) || TryLink(rightText, eventData))
        {
            Refresh();
        }
    }

    private bool TryLink(TMP_Text label, PointerEventData eventData)
    {
        if (label == null) return false;

        int index = TMP_TextUtilities.FindIntersectingLink(label, eventData.position, eventData.pressEventCamera);
        if (index < 0 || index >= label.textInfo.linkInfo.Length) return false;

        string link = label.textInfo.linkInfo[index].GetLinkID();
        if (string.IsNullOrEmpty(link)) return false;

        string[] parts = link.Split(':');
        if (parts.Length == 0) return false;

        switch (parts[0])
        {
            case "product":
                if (parts.Length >= 3)
                {
                    int delta;
                    if (int.TryParse(parts[2], out delta)) book.ChangeProductUnits(parts[1], delta);
                }
                return true;

            case "jar":
                if (parts.Length >= 3)
                {
                    PlannerJarKind jar;
                    int delta;
                    if (System.Enum.TryParse(parts[1], out jar) && int.TryParse(parts[2], out delta))
                    {
                        book.ChangeJar(jar, delta);
                    }
                }
                return true;

            case "band":
                if (parts.Length >= 3)
                {
                    int band;
                    if (int.TryParse(parts[2], out band)) book.SetPriceBand(parts[1], band);
                }
                return true;

            case "step":
                if (parts.Length >= 2)
                {
                    int step;
                    if (int.TryParse(parts[1], out step)) book.ToggleFirstStep(step);
                }
                return true;

            case "deliver":
                if (parts.Length >= 2) book.DeliverCommission(parts[1]);
                return true;
        }

        return false;
    }

    // ---------------------------------------------------------------------
    // Method tombol manual tetap tersedia untuk OnClick di Inspector.
    // ---------------------------------------------------------------------

    public void AddProductUnit(string productId)
    {
        if (book != null) book.ChangeProductUnits(productId, 1);
    }

    public void RemoveProductUnit(string productId)
    {
        if (book != null) book.ChangeProductUnits(productId, -1);
    }

    public void SetProductUnits(string productIdAndUnits)
    {
        if (book == null || string.IsNullOrEmpty(productIdAndUnits)) return;
        string[] parts = productIdAndUnits.Split(':');
        int units;
        if (parts.Length == 2 && int.TryParse(parts[1], out units))
        {
            book.SetProductUnits(parts[0], units);
        }
    }

    public void AddStoreJar() { if (book != null) book.ChangeJar(PlannerJarKind.Store, 5); }
    public void RemoveStoreJar() { if (book != null) book.ChangeJar(PlannerJarKind.Store, -5); }
    public void AddPersonalJar() { if (book != null) book.ChangeJar(PlannerJarKind.Personal, 5); }
    public void RemovePersonalJar() { if (book != null) book.ChangeJar(PlannerJarKind.Personal, -5); }
    public void AddBufferJar() { if (book != null) book.ChangeJar(PlannerJarKind.Buffer, 5); }
    public void RemoveBufferJar() { if (book != null) book.ChangeJar(PlannerJarKind.Buffer, -5); }

    public void SetMarketBand(string itemAndBand)
    {
        if (book == null || string.IsNullOrEmpty(itemAndBand)) return;
        string[] parts = itemAndBand.Split(':');
        int band;
        if (parts.Length == 2 && int.TryParse(parts[1], out band))
        {
            book.SetPriceBand(parts[0], band);
        }
    }

    public void ToggleFirstStep(int index)
    {
        if (book != null) book.ToggleFirstStep(index);
    }

    public void DeliverCommission(string commissionId)
    {
        if (book != null) book.DeliverCommission(commissionId);
    }
}
