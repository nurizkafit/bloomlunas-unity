using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengisi teks dan tombol pada UI halaman yang sudah ada di Hierarchy.
/// Script ini tidak membuat row, panel, atau tombol baru.
/// </summary>
public class PlannerPageBinder : MonoBehaviour
{
    [SerializeField] private PlannerPageId pageId;

    private PlannerBook book;

    private void OnEnable()
    {
        book = PlannerBook.Instance;
        if (book != null) book.PlannerChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (book != null) book.PlannerChanged -= Refresh;
    }

    public void Refresh()
    {
        if (!isActiveAndEnabled) return;
        if (book == null) book = PlannerBook.Instance;
        if (book == null) return;

        List<Transform> roots = Roots();
        for (int i = 0; i < roots.Count; i++)
        {
            switch (pageId)
            {
                case PlannerPageId.Store: RefreshStore(roots[i]); break;
                case PlannerPageId.Ledger: RefreshLedger(roots[i]); break;
                case PlannerPageId.Market: RefreshMarket(roots[i]); break;
                case PlannerPageId.Hours: RefreshHours(roots[i]); break;
                case PlannerPageId.Skills: RefreshSkills(roots[i]); break;
                case PlannerPageId.Notes: RefreshNotes(roots[i]); break;
                case PlannerPageId.Quest: RefreshQuest(roots[i]); break;
            }
        }
    }

    private List<Transform> Roots()
    {
        List<Transform> roots = new List<Transform>();
        Transform page = transform.Find("Page");
        if (pageId == PlannerPageId.Store && page != null)
        {
            Transform decor = page.Find("Decor");
            if (decor != null) roots.Add(decor);
            if (roots.Count > 0) return roots;
        }

        // Sebagian halaman punya PageContent langsung, sebagian lagi di
        // bawah Page. Market bahkan punya dua PageContent yang sama-sama aktif.
        Transform direct = transform.Find("PageContent");
        if (direct != null) roots.Add(direct);
        if (page != null)
        {
            for (int i = 0; i < page.childCount; i++)
            {
                Transform child = page.GetChild(i);
                if (child.name == "PageContent" && !roots.Contains(child)) roots.Add(child);
            }
        }

        if (roots.Count == 0) roots.Add(transform);
        return roots;
    }

    private List<Transform> Columns(Transform root)
    {
        List<Transform> columns = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == "Column") columns.Add(child);
        }
        if (columns.Count == 0) columns.Add(root);
        return columns;
    }

    private List<Transform> Rows(Transform container)
    {
        List<Transform> rows = new List<Transform>();
        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            int textCount = Labels(child).Count;
            if (textCount >= 2 || child.name.StartsWith("Row") ||
                child.GetComponentInChildren<Button>(true) != null)
            {
                rows.Add(child);
            }
        }
        return rows;
    }

    private List<TMP_Text> Labels(Transform row)
    {
        List<TMP_Text> labels = new List<TMP_Text>();
        TMP_Text[] all = row.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].GetComponentInParent<Button>() == null) labels.Add(all[i]);
        }
        return labels;
    }

    private void SetRow(Transform row, string title, string value, string extra = null)
    {
        if (row == null) return;
        List<TMP_Text> labels = Labels(row);
        if (labels.Count > 0) labels[0].text = title;
        if (labels.Count > 1) labels[1].text = value;
        if (labels.Count > 2) labels[2].text = extra ?? string.Empty;
        else if (labels.Count == 2 && !string.IsNullOrEmpty(extra))
        {
            labels[1].text = value + "  " + extra;
        }
    }

    private void SetSection(Transform container, int index, string text)
    {
        if (container == null) return;
        TMP_Text[] labels = container.GetComponentsInChildren<TMP_Text>(true);
        int found = 0;
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].GetComponentInParent<Button>() != null) continue;
            if (found == index)
            {
                labels[i].text = text;
                return;
            }
            found++;
        }
    }

    private void SetButtonLabel(Button button, string text)
    {
        if (button == null) return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }

    private void WireRowActions(Transform row, Action[] actions)
    {
        if (row == null || actions == null) return;
        Button[] buttons = row.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length && i < actions.Length; i++)
        {
            Button button = buttons[i];
            Action action = actions[i];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }
    }

    private void RefreshStore(Transform root)
    {
        List<Transform> rows = Rows(root);

        int productIndex = 0;
        for (int i = 0; i < book.Products.Count && productIndex < rows.Count; i++)
        {
            PlannerProductOffer product = book.Products[i];
            string id = product.productId;
            Transform row = rows[productIndex++];
            SetRow(row, product.displayName, "demand " + product.demandMin + "-" + product.demandMax,
                "each " + book.GetPrice(id).ToString("0.0") + " · " + product.units + "u");
            WireRowActions(row, new Action[]
            {
                () => book.ChangeProductUnits(id, -1),
                () => book.ChangeProductUnits(id, 1)
            });
        }

        int herbIndex = productIndex;
        int herbLimit = Mathf.Min(3, rows.Count - herbIndex);
        int herbsFilled = 0;
        for (int i = 0; i < book.Products.Count && herbsFilled < herbLimit; i++)
        {
            PlannerProductOffer product = book.Products[i];
            for (int j = 0; j < product.ingredients.Count && herbsFilled < herbLimit; j++)
            {
                PlannerIngredientNeed need = product.ingredients[j];
                int have = book.GetAmount(need.itemId);
                SetRow(rows[herbIndex++], need.displayName, have + "/" + need.needed);
                herbsFilled++;
            }
        }

        int jarIndex = productIndex + Mathf.Max(herbsFilled, herbLimit);
        if (jarIndex + 2 < rows.Count)
        {
            SetRow(rows[jarIndex], "Store", book.Budget.store.ToString());
            SetRow(rows[jarIndex + 1], "Personal", book.Budget.personal.ToString());
            SetRow(rows[jarIndex + 2], "Buffer", book.Budget.buffer.ToString());
            WireRowActions(rows[jarIndex], new Action[] { () => book.ChangeJar(PlannerJarKind.Store, -5), () => book.ChangeJar(PlannerJarKind.Store, 5) });
            WireRowActions(rows[jarIndex + 1], new Action[] { () => book.ChangeJar(PlannerJarKind.Personal, -5), () => book.ChangeJar(PlannerJarKind.Personal, 5) });
            WireRowActions(rows[jarIndex + 2], new Action[] { () => book.ChangeJar(PlannerJarKind.Buffer, -5), () => book.ChangeJar(PlannerJarKind.Buffer, 5) });
        }

        int summaryIndex = jarIndex + 3;
        if (summaryIndex + 2 < rows.Count)
        {
            SetRow(rows[summaryIndex], "In the jars", book.JarTotal + " / " + book.Gold);
            SetRow(rows[summaryIndex + 1], "Living cost", "-" + book.WeeklyLivingCost);
            SetRow(rows[summaryIndex + 2], "Gold after plan", book.GoldAfterPlan.ToString());
        }
    }

    private void RefreshLedger(Transform root)
    {
        List<Transform> columns = Columns(root);
        if (columns.Count < 2) return;
        List<Transform> left = Rows(columns[0]);
        List<Transform> right = Rows(columns[1]);

        if (left.Count > 0) SetRow(left[0], "Earned", "+" + book.WeekEarned);
        if (left.Count > 1) SetRow(left[1], "Spent", "-" + book.WeekSpent);
        if (left.Count > 2) SetRow(left[2], "Store", book.JarSpent(PlannerJarKind.Store) + " / " + book.Budget.store);
        if (left.Count > 3) SetRow(left[3], "Personal", book.JarSpent(PlannerJarKind.Personal) + " / " + book.Budget.personal);
        if (left.Count > 4) SetRow(left[4], "Buffer", book.JarSpent(PlannerJarKind.Buffer) + " / " + book.Budget.buffer);
        if (left.Count > 5)
        {
            if (book.Ledger.Count == 0) SetRow(left[5], "No entry yet.", string.Empty);
            else SetRow(left[5], book.Ledger[book.Ledger.Count - 1].description,
                book.Ledger[book.Ledger.Count - 1].amount.ToString());
        }

        if (right.Count > 0) SetRow(right[0], "Gold", book.Gold.ToString());
        if (right.Count > 1) SetRow(right[1], "Debt", book.Debt.ToString());
        int rowIndex = 2;
        for (int i = 0; i < book.Inventory.Count && rowIndex < right.Count; i++)
        {
            SetRow(right[rowIndex++], book.Inventory[i].displayName, "x" + book.Inventory[i].amount);
        }
    }

    private void RefreshMarket(Transform root)
    {
        List<Transform> columns = Columns(root);
        if (columns.Count < 2) return;
        List<Transform> left = Rows(columns[0]);
        List<Transform> right = Rows(columns[1]);

        int leftIndex = 0;
        for (int i = 0; i < book.MarketItems.Count && leftIndex < left.Count; i++)
        {
            PlannerMarketItem item = book.MarketItems[i];
            if (!item.isProduct) continue;
            string id = item.itemId;
            Transform row = left[leftIndex++];
            SetRow(row, item.displayName, book.GetPrice(id).ToString("0.0"));
            WireRowActions(row, new Action[]
            {
                () => book.SetPriceBand(id, 0),
                () => book.SetPriceBand(id, 1),
                () => book.SetPriceBand(id, 2)
            });
        }

        int rightIndex = 0;
        for (int i = 0; i < book.MarketItems.Count && rightIndex < right.Count; i++)
        {
            PlannerMarketItem item = book.MarketItems[i];
            if (item.isProduct) continue;
            string id = item.itemId;
            Transform row = right[rightIndex++];
            SetRow(row, item.displayName, book.GetPrice(id).ToString("0.0"));
            WireRowActions(row, new Action[]
            {
                () => book.SetPriceBand(id, 0),
                () => book.SetPriceBand(id, 1),
                () => book.SetPriceBand(id, 2)
            });
        }
    }

    private void RefreshHours(Transform root)
    {
        List<Transform> columns = Columns(root);
        if (columns.Count < 2) return;
        List<Transform> left = Rows(columns[0]);
        List<Transform> right = Rows(columns[1]);
        for (int i = 0; i < book.Schedule.Count && i < left.Count; i++)
        {
            PlannerScheduleEntry entry = book.Schedule[i];
            string hours = entry.openHour == 0 && entry.closeHour >= 24 ? "24h" : entry.openHour + "-" + entry.closeHour;
            SetRow(left[i], entry.locationName, hours);
        }
        if (right.Count > 0) SetRow(right[0], "Commissions", "Monday");
        if (right.Count > 1) SetRow(right[1], "Living costs", "-" + book.WeeklyLivingCost);
        if (right.Count > 2) SetRow(right[2], "Debt minimum", book.MinimumDebtPayment.ToString());
        if (right.Count > 3) SetRow(right[3], "Utilities", book.MonthlyUtilities.ToString());
    }

    private void RefreshSkills(Transform root)
    {
        List<Transform> columns = Columns(root);
        if (columns.Count < 2) return;
        List<Transform> left = Rows(columns[0]);
        List<Transform> right = Rows(columns[1]);
        AddSkill(left, 0, PlannerSkillBranch.Moon, "Moon");
        AddSkill(left, 1, PlannerSkillBranch.Sun, "Sun");
        AddSkill(left, 2, PlannerSkillBranch.Earth, "Earth");
        if (right.Count > 0) SetRow(right[0], "Moon: better products", "Lv 3");
        if (right.Count > 1) SetRow(right[1], "Sun: more customers", "Lv 2");
        if (right.Count > 2) SetRow(right[2], "Earth: more herbs", "Lv 2");
        if (right.Count > 3) SetRow(right[3], "XP per goal", book.SkillXpPerGoal.ToString());
    }

    private void AddSkill(List<Transform> rows, int index, PlannerSkillBranch branch, string name)
    {
        if (index >= rows.Count) return;
        SetRow(rows[index], name, "Lv " + book.GetSkillLevel(branch) + "  " + book.GetSkillXp(branch) + "/" + book.SkillXpForNextLevel);
    }

    private void RefreshNotes(Transform root)
    {
        List<Transform> columns = Columns(root);
        if (columns.Count < 2) return;

        TMP_Text[] leftTexts = columns[0].GetComponentsInChildren<TMP_Text>(true);
        TMP_Text note = null;
        for (int i = 0; i < leftTexts.Length; i++)
        {
            if (leftTexts[i].GetComponentInParent<Button>() == null && leftTexts[i].name != "Label")
            {
                note = leftTexts[i];
                break;
            }
        }
        if (note != null) note.text = book.LastWeekNote;
        else if (leftTexts.Length > 1) leftTexts[1].text = book.LastWeekNote;

        List<Transform> rows = Rows(columns[1]);
        for (int i = 0; i < rows.Count && i < book.FirstSteps.Count; i++)
        {
            int index = i;
            PlannerFirstStep step = book.FirstSteps[i];
            SetRow(rows[i], step.text, step.done ? "done" : "open");
            Button button = rows[i].GetComponentInChildren<Button>(true);
            if (button != null)
            {
                SetButtonLabel(button, step.done ? "x" : "o");
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => book.ToggleFirstStep(index));
            }
        }
    }

    private void RefreshQuest(Transform root)
    {
        List<Transform> columns = Columns(root);
        if (columns.Count < 2) return;
        TMP_Text[] left = columns[0].GetComponentsInChildren<TMP_Text>(true);
        TMP_Text[] right = columns[1].GetComponentsInChildren<TMP_Text>(true);
        if (left.Length > 0) left[0].text = "TAKEN COMMISSIONS  " + book.Commissions.Count;
        if (left.Length > 1)
        {
            left[left.Length - 1].text = book.Commissions.Count == 0 ? "No commission yet." : book.Commissions[0].title;
        }
        if (right.Length > 0) right[0].text = "YOUR REQUESTS  " + book.Requests.Count;
        if (right.Length > 1)
        {
            right[right.Length - 1].text = book.Requests.Count == 0
                ? "No open request."
                : book.Requests[0].itemId + " x" + book.Requests[0].quantity;
        }
    }
}
