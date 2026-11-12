using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Halaman yang bisa dibuka lewat TabRail.</summary>
public enum PlannerPageId
{
    Goals,
    Store,
    Ledger,
    Market,
    Hours,
    Skills,
    Notes,
    Quest
}

/// <summary>Jenis kartu goal yang tersedia di dek planner.</summary>
public enum PlannerGoalType
{
    Explore,
    Craft,
    OpenShop,
    MarketGuild,
    FreeTime,
    Custom
}

/// <summary>Cabang skill yang dipakai planner dan CoreLoop.</summary>
public enum PlannerSkillBranch
{
    None,
    Moon,
    Sun,
    Earth
}

/// <summary>Pita harga di halaman Market.</summary>
public enum PlannerPriceBand
{
    Low,
    Fair,
    High
}

public enum PlannerMarketTrend
{
    Down,
    Stable,
    Up
}

public enum PlannerLedgerKind
{
    Earn,
    Spend,
    Neutral
}

public enum PlannerCommissionStatus
{
    Open,
    Taken,
    Ready,
    Delivered,
    Expired
}

public enum PlannerRequestStatus
{
    Posted,
    Arrived,
    Claimed,
    Expired
}

public enum PlannerJarKind
{
    Store,
    Personal,
    Buffer
}

/// <summary>
/// Isi satu kartu goal. Nama field pertama sengaja sama dengan yang sudah
/// tersimpan di Card_Prefab supaya data lama tidak hilang saat script dibuat ulang.
/// </summary>
[Serializable]
public class PlannerGoalCardData
{
    public string activityId = "exploration";
    public string activityName = "Explore";
    public string category = "Fieldwork";
    public string locationName = "City Outskirt";
    public string timeSlot = "06:00";
    public string subtext = "3h - City Outskirt";
    public Color cardColor = new Color(0.96f, 0.94f, 0.88f, 1f);
    public int slotsCount = 1;
    public string selectedRecipe = "Beras kencur";
    public string selectedStation = "Home atelier";

    [Header("Data Planner")]
    public PlannerGoalType goalType = PlannerGoalType.Explore;
    public PlannerSkillBranch skillBranch = PlannerSkillBranch.Earth;
    public Color accentColor = new Color(0.64f, 0.75f, 0.48f, 1f);

    public PlannerGoalCardData Clone()
    {
        return new PlannerGoalCardData
        {
            activityId = activityId,
            activityName = activityName,
            category = category,
            locationName = locationName,
            timeSlot = timeSlot,
            subtext = subtext,
            cardColor = cardColor,
            slotsCount = slotsCount,
            selectedRecipe = selectedRecipe,
            selectedStation = selectedStation,
            goalType = goalType,
            skillBranch = skillBranch,
            accentColor = accentColor
        };
    }
}

[Serializable]
public class PlannerDayPlan
{
    public int dayIndex;
    public bool hasGoal;
    public PlannerGoalCardData goal;
    public bool completed;
    public bool missed;
}

[Serializable]
public class PlannerWeekPlan
{
    public int month = 1;
    public int week = 1;
    public List<PlannerDayPlan> days = new List<PlannerDayPlan>();
}

[Serializable]
public class PlannerSkillProgress
{
    public PlannerSkillBranch branch = PlannerSkillBranch.Moon;
    public int level = 1;
    public int xp;
}

[Serializable]
public class PlannerIngredientNeed
{
    public string itemId;
    public string displayName;
    public int needed;
}

[Serializable]
public class PlannerProductOffer
{
    public string productId;
    public string displayName;
    public int demandMin;
    public int demandMax;
    public int usualPrice;
    public int units;
    public List<PlannerIngredientNeed> ingredients = new List<PlannerIngredientNeed>();
}

[Serializable]
public class PlannerMarketItem
{
    public string itemId;
    public string displayName;
    public bool isProduct;
    public float demandMin;
    public float demandMax;
    public float usualPrice;
    public float nowPrice;
    public PlannerMarketTrend trend = PlannerMarketTrend.Stable;
    public PlannerPriceBand priceBand = PlannerPriceBand.Fair;
}

[Serializable]
public class PlannerBudget
{
    public int store = 100;
    public int personal = 63;
    public int buffer = 70;

    public int Total => store + personal + buffer;
}

[Serializable]
public class PlannerLedgerEntry
{
    public string weekLabel;
    public int dayIndex;
    public string description;
    public int amount;
    public PlannerLedgerKind kind;
}

[Serializable]
public class PlannerInventoryItem
{
    public string itemId;
    public string displayName;
    public int amount;
    public bool isProduct;
}

[Serializable]
public class PlannerScheduleEntry
{
    public string locationName;
    public int openHour = 6;
    public int closeHour = 18;
    [Tooltip("-1 berarti buka setiap hari.")]
    public int closedDay = -1;
    public int walkMinutes;
}

[Serializable]
public class PlannerCommission
{
    public string commissionId;
    public string title;
    public string itemId;
    public int required = 1;
    public int have;
    public int reward;
    public int deadlineDay = 6;
    public PlannerCommissionStatus status = PlannerCommissionStatus.Open;
}

[Serializable]
public class PlannerRequest
{
    public string requestId;
    public string itemId;
    public int quantity = 1;
    public int arrivalDay;
    public int reward;
    public PlannerRequestStatus status = PlannerRequestStatus.Posted;
}

[Serializable]
public class PlannerFirstStep
{
    public string text;
    public bool done;
}
