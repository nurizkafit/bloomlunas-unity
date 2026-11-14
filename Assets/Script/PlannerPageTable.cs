using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengganti teks Body halaman dengan baris UI yang dibangun dari sprite UI
/// yang sudah ada. Layout ini hanya memakai font dan sprite dari scene.
/// </summary>
public class PlannerPageTable : MonoBehaviour
{
    [SerializeField] private PlannerPageId pageId;
    [SerializeField] private PlannerPageTheme theme;

    private PlannerBook book;
    private TMP_Text sourceText;
    private RectTransform content;
    private bool configured;

    private static readonly Color Ink = new Color(0.169f, 0.141f, 0.106f, 1f);
    private static readonly Color Muted = new Color(0.48f, 0.42f, 0.31f, 1f);
    private static readonly Color Accent = new Color(0.184f, 0.369f, 0.306f, 1f);
    private static readonly Color Red = new Color(0.64f, 0.23f, 0.18f, 1f);
    private static readonly Color Green = new Color(0.184f, 0.42f, 0.31f, 1f);

    public void Configure(PlannerPageId id, PlannerPageTheme pageTheme)
    {
        pageId = id;
        theme = pageTheme;
        configured = true;

        if (isActiveAndEnabled) Rebuild();
    }

    private void OnEnable()
    {
        if (!configured) return;

        book = PlannerBook.Instance;
        if (book != null) book.PlannerChanged += Rebuild;
        Rebuild();
    }

    private void OnDisable()
    {
        if (book != null) book.PlannerChanged -= Rebuild;
    }

    private void Rebuild()
    {
        if (!configured || !isActiveAndEnabled || theme == null) return;
        if (book == null) book = PlannerBook.Instance;
        if (book == null) return;

        HideBodies();
        if (sourceText == null) sourceText = FindBody();
        if (sourceText == null) return;

        EnsureContent();
        ClearContent();
        switch (pageId)
        {
            case PlannerPageId.Store: BuildStore(); break;
            case PlannerPageId.Ledger: BuildLedger(); break;
            case PlannerPageId.Market: BuildMarket(); break;
            case PlannerPageId.Hours: BuildHours(); break;
            case PlannerPageId.Skills: BuildSkills(); break;
            case PlannerPageId.Notes: BuildNotes(); break;
            case PlannerPageId.Quest: BuildQuest(); break;
        }
    }

    private TMP_Text FindBody()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == "Body") return texts[i];
        }
        return null;
    }

    private void HideBodies()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == "Body") texts[i].gameObject.SetActive(false);
        }
    }

    private void EnsureContent()
    {
        if (content != null) return;

        Transform parent = transform.Find("Page");
        if (parent == null) parent = transform;

        GameObject root = new GameObject("PageContent", typeof(RectTransform));
        content = root.GetComponent<RectTransform>();
        content.SetParent(parent, false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(44f, 36f);
        content.offsetMax = new Vector2(-44f, -150f);

        HorizontalLayoutGroup horizontal = root.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = 18f;
        horizontal.padding = new RectOffset(0, 0, 0, 0);
        horizontal.childAlignment = TextAnchor.UpperLeft;
        horizontal.childForceExpandWidth = true;
        horizontal.childForceExpandHeight = true;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
    }

    private void ClearContent()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    private RectTransform Column()
    {
        GameObject go = new GameObject("Column", typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(content, false);

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.flexibleHeight = 1f;
        element.minWidth = 0f;
        element.minHeight = 80f;

        VerticalLayoutGroup vertical = go.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 5f;
        vertical.padding = new RectOffset(0, 0, 0, 0);
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        return rect;
    }

    private void Section(RectTransform column, string title)
    {
        TMP_Text text = Label(column, title, 20, Accent, FontStyles.Bold);
        text.characterSpacing = 0.4f;
        LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 28f;
        element.minHeight = 28f;
    }

    private GameObject Row(RectTransform column, Sprite icon, string title, string value,
        string[] actions, Action[] callbacks)
    {
        GameObject go = new GameObject("Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(column, false);

        Image background = go.GetComponent<Image>();
        background.sprite = theme.rowBackground;
        background.type = Image.Type.Sliced;
        background.color = Color.white;
        background.raycastTarget = false;

        LayoutElement rowElement = go.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 52f;
        rowElement.minHeight = 52f;

        HorizontalLayoutGroup horizontal = go.AddComponent<HorizontalLayoutGroup>();
        horizontal.padding = new RectOffset(9, 9, 2, 2);
        horizontal.spacing = 6f;
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = false;

        if (icon != null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rect, false);
            Image image = iconObject.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            LayoutElement iconElement = iconObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = 28f;
            iconElement.preferredHeight = 28f;
            iconElement.minWidth = 28f;
        }

        TMP_Text titleText = Label(rect, title, 19, Ink, FontStyles.Normal);
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        LayoutElement titleElement = titleText.gameObject.AddComponent<LayoutElement>();
        titleElement.flexibleWidth = 1f;
        titleElement.minWidth = 20f;

        if (!string.IsNullOrEmpty(value))
        {
            TMP_Text valueText = Label(rect, value, 19, Muted, FontStyles.Bold);
            valueText.horizontalAlignment = HorizontalAlignmentOptions.Right;
            LayoutElement valueElement = valueText.gameObject.AddComponent<LayoutElement>();
            valueElement.preferredWidth = 74f;
            valueElement.minWidth = 74f;
        }

        if (actions != null && callbacks != null)
        {
            for (int i = 0; i < actions.Length && i < callbacks.Length; i++)
            {
                string label = actions[i];
                Action callback = callbacks[i];
                GameObject buttonObject = new GameObject("Action", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.SetParent(rect, false);

                Image buttonImage = buttonObject.GetComponent<Image>();
                buttonImage.sprite = theme.button;
                buttonImage.type = Image.Type.Sliced;
                buttonImage.color = Color.white;
                buttonImage.raycastTarget = true;

                Button button = buttonObject.GetComponent<Button>();
                button.targetGraphic = buttonImage;
                button.onClick.AddListener(() => callback());

                TMP_Text buttonText = Label(buttonRect, label, 19, Ink, FontStyles.Bold);
                buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;
                buttonText.verticalAlignment = VerticalAlignmentOptions.Middle;
                buttonText.rectTransform.anchorMin = Vector2.zero;
                buttonText.rectTransform.anchorMax = Vector2.one;
                buttonText.rectTransform.offsetMin = Vector2.zero;
                buttonText.rectTransform.offsetMax = Vector2.zero;

                LayoutElement buttonElement = buttonObject.AddComponent<LayoutElement>();
                buttonElement.preferredWidth = 32f;
                buttonElement.preferredHeight = 32f;
                buttonElement.minWidth = 32f;
            }
        }

        return go;
    }

    private TMP_Text Label(Transform parent, string text, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (sourceText != null) label.font = sourceText.font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Left;
        label.horizontalAlignment = HorizontalAlignmentOptions.Left;
        label.verticalAlignment = VerticalAlignmentOptions.Middle;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private void TextBlock(RectTransform column, string text, float height)
    {
        TMP_Text label = Label(column, text, 18, Ink, FontStyles.Normal);
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.verticalAlignment = VerticalAlignmentOptions.Top;
        LayoutElement element = label.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
    }

    private void BuildStore()
    {
        RectTransform left = Column();
        RectTransform right = Column();

        Section(left, "PRODUCTS TO SELL");
        for (int i = 0; i < book.Products.Count; i++)
        {
            PlannerProductOffer product = book.Products[i];
            string id = product.productId;
            Row(left, theme.iconProduct, product.displayName, product.units.ToString(),
                new[] { "-", "+" },
                new Action[] { () => book.ChangeProductUnits(id, -1), () => book.ChangeProductUnits(id, 1) });
        }

        Section(left, "HERBS YOU NEED");
        for (int i = 0; i < book.Products.Count; i++)
        {
            PlannerProductOffer product = book.Products[i];
            if (product.units <= 0) continue;
            for (int j = 0; j < product.ingredients.Count; j++)
            {
                PlannerIngredientNeed need = product.ingredients[j];
                int have = book.GetAmount(need.itemId);
                Row(left, theme.iconHerb, need.displayName, have + "/" + need.needed, null, null);
            }
        }

        Section(right, "BUDGET");
        AddJarRow(right, "Store", PlannerJarKind.Store, book.Budget.store);
        AddJarRow(right, "Personal", PlannerJarKind.Personal, book.Budget.personal);
        AddJarRow(right, "Buffer", PlannerJarKind.Buffer, book.Budget.buffer);

        Section(right, "SUMMARY");
        Row(right, theme.iconGold, "In the jars", book.JarTotal + " / " + book.Gold, null, null);
        Row(right, theme.iconDebt, "Living cost", "-" + book.WeeklyLivingCost, null, null);
        Row(right, theme.iconGold, "Gold after plan", book.GoldAfterPlan.ToString(), null, null);
    }

    private void AddJarRow(RectTransform column, string name, PlannerJarKind kind, int amount)
    {
        PlannerJarKind localKind = kind;
        Row(column, theme.iconJar, name, amount.ToString(), new[] { "-", "+" },
            new Action[] { () => book.ChangeJar(localKind, -5), () => book.ChangeJar(localKind, 5) });
    }

    private void BuildLedger()
    {
        RectTransform left = Column();
        RectTransform right = Column();

        Section(left, "THIS WEEK");
        Row(left, theme.iconGold, "Earned", "+" + book.WeekEarned, null, null);
        Row(left, theme.iconDebt, "Spent", "-" + book.WeekSpent, null, null);

        Section(left, "JARS SPENT");
        Row(left, theme.iconJar, "Store", book.JarSpent(PlannerJarKind.Store) + " / " + book.Budget.store, null, null);
        Row(left, theme.iconJar, "Personal", book.JarSpent(PlannerJarKind.Personal) + " / " + book.Budget.personal, null, null);
        Row(left, theme.iconJar, "Buffer", book.JarSpent(PlannerJarKind.Buffer) + " / " + book.Budget.buffer, null, null);

        Section(left, "LATEST ENTRIES");
        if (book.Ledger.Count == 0) Row(left, theme.iconNote, "No entry yet.", null, null, null);
        for (int i = Mathf.Max(0, book.Ledger.Count - 4); i < book.Ledger.Count; i++)
        {
            PlannerLedgerEntry entry = book.Ledger[i];
            string sign = entry.kind == PlannerLedgerKind.Earn ? "+" : entry.kind == PlannerLedgerKind.Spend ? "-" : "";
            Row(left, theme.iconGold, entry.description, sign + entry.amount, null, null);
        }

        Section(right, "WHAT YOU OWN");
        Row(right, theme.iconGold, "Gold", book.Gold.ToString(), null, null);
        Row(right, theme.iconDebt, "Debt", book.Debt.ToString(), null, null);

        Section(right, "PRODUCTS");
        for (int i = 0; i < book.Inventory.Count; i++)
        {
            if (!book.Inventory[i].isProduct) continue;
            Row(right, theme.iconProduct, book.Inventory[i].displayName, "x" + book.Inventory[i].amount, null, null);
        }

        Section(right, "MATERIALS");
        for (int i = 0; i < book.Inventory.Count; i++)
        {
            if (book.Inventory[i].isProduct) continue;
            Row(right, theme.iconHerb, book.Inventory[i].displayName, "x" + book.Inventory[i].amount, null, null);
        }
    }

    private void BuildMarket()
    {
        RectTransform left = Column();
        RectTransform right = Column();

        Section(left, "PRODUCTS");
        for (int i = 0; i < book.MarketItems.Count; i++)
        {
            PlannerMarketItem item = book.MarketItems[i];
            if (!item.isProduct) continue;
            string id = item.itemId;
            Row(left, theme.iconProduct, item.displayName, book.GetPrice(id).ToString("0.0"),
                new[] { "L", "F", "H" },
                new Action[] { () => book.SetPriceBand(id, 0), () => book.SetPriceBand(id, 1), () => book.SetPriceBand(id, 2) });
        }

        Section(right, "HERBS AND MATERIALS");
        for (int i = 0; i < book.MarketItems.Count; i++)
        {
            PlannerMarketItem item = book.MarketItems[i];
            if (item.isProduct) continue;
            string id = item.itemId;
            Row(right, theme.iconHerb, item.displayName, book.GetPrice(id).ToString("0.0"),
                new[] { "L", "F", "H" },
                new Action[] { () => book.SetPriceBand(id, 0), () => book.SetPriceBand(id, 1), () => book.SetPriceBand(id, 2) });
        }
    }

    private void BuildHours()
    {
        RectTransform left = Column();
        RectTransform right = Column();

        Section(left, "OPENING HOURS");
        for (int i = 0; i < book.Schedule.Count; i++)
        {
            PlannerScheduleEntry entry = book.Schedule[i];
            string hours = entry.openHour == 0 && entry.closeHour >= 24
                ? "24h"
                : entry.openHour + "-" + entry.closeHour;
            string closed = entry.closedDay >= 0 ? "closed " + CoreLoop.DayNames[entry.closedDay] : "walk " + entry.walkMinutes + "m";
            Row(left, theme.iconClock, entry.locationName, hours, null, null);
            TextBlock(left, closed, 20f);
        }

        Section(right, "WEEK " + (book.CurrentWeek != null ? book.CurrentWeek.week : 1));
        Row(right, theme.iconClock, "Commissions", "Monday", null, null);
        Row(right, theme.iconDebt, "Living costs", "-" + book.WeeklyLivingCost, null, null);
        Row(right, theme.iconDebt, "Debt minimum", book.MinimumDebtPayment.ToString(), null, null);
        Row(right, theme.iconGold, "Utilities", book.MonthlyUtilities.ToString(), null, null);
    }

    private void BuildSkills()
    {
        RectTransform left = Column();
        RectTransform right = Column();

        Section(left, "SKILLS");
        AddSkillRow(left, PlannerSkillBranch.Moon, "Moon", theme.iconMoon);
        AddSkillRow(left, PlannerSkillBranch.Sun, "Sun", theme.iconSun);
        AddSkillRow(left, PlannerSkillBranch.Earth, "Earth", theme.iconEarth);

        Section(right, "WHAT THEY GIVE");
        Row(right, theme.iconMoon, "Moon: better products", "Lv 3", null, null);
        Row(right, theme.iconSun, "Sun: more customers", "Lv 2", null, null);
        Row(right, theme.iconEarth, "Earth: more herbs", "Lv 2", null, null);
        Row(right, theme.iconGold, "XP per goal", book.SkillXpPerGoal.ToString(), null, null);
    }

    private void AddSkillRow(RectTransform column, PlannerSkillBranch branch, string name, Sprite icon)
    {
        Row(column, icon, name, "Lv " + book.GetSkillLevel(branch) + "  " + book.GetSkillXp(branch) + "/" + book.SkillXpForNextLevel, null, null);
    }

    private void BuildNotes()
    {
        RectTransform left = Column();
        RectTransform right = Column();

        Section(left, "FROM LAST WEEK");
        TextBlock(left, string.IsNullOrEmpty(book.LastWeekNote) ? "You left no note last week." : book.LastWeekNote, 100f);

        Section(right, "FIRST STEPS  " + book.CompletedFirstSteps + "/" + book.FirstSteps.Count);
        for (int i = 0; i < book.FirstSteps.Count; i++)
        {
            int index = i;
            PlannerFirstStep step = book.FirstSteps[i];
            Row(right, step.done ? theme.iconCheck : theme.iconEmpty, step.text, step.done ? "done" : "open",
                new[] { step.done ? "x" : "o" },
                new Action[] { () => book.ToggleFirstStep(index) });
        }
    }

    private void BuildQuest()
    {
        RectTransform left = Column();
        RectTransform right = Column();

        Section(left, "TAKEN COMMISSIONS  " + book.Commissions.Count);
        if (book.Commissions.Count == 0)
        {
            Row(left, theme.iconQuest, "No commission yet.", null, null, null);
        }
        else
        {
            for (int i = 0; i < book.Commissions.Count; i++)
            {
                PlannerCommission commission = book.Commissions[i];
                string id = commission.commissionId;
                int have = book.GetAmount(commission.itemId);
                bool ready = have >= commission.required;
                Row(left, theme.iconQuest, commission.title, have + "/" + commission.required,
                    ready ? new[] { "Deliver" } : null,
                    ready ? new Action[] { () => book.DeliverCommission(id) } : null);
            }
        }

        Section(right, "YOUR REQUESTS  " + book.Requests.Count);
        if (book.Requests.Count == 0)
        {
            Row(right, theme.iconNote, "No open request.", null, null, null);
        }
        else
        {
            for (int i = 0; i < book.Requests.Count; i++)
            {
                PlannerRequest request = book.Requests[i];
                Row(right, theme.iconNote, request.itemId + " x" + request.quantity,
                    "D" + (request.arrivalDay + 1), null, null);
            }
        }
    }
}
