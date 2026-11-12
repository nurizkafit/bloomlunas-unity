using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data awal Buku Planner. Semua isi di sini adalah data biasa; tidak ada
/// GameObject, Canvas, atau UI yang dibuat saat Play.
/// </summary>
public static class PlannerCatalog
{
    public static PlannerGoalCardData GetGoal(PlannerGoalType type)
    {
        switch (type)
        {
            case PlannerGoalType.Craft:
                return new PlannerGoalCardData
                {
                    activityId = "crafting",
                    activityName = "Craft",
                    category = "Workshop",
                    locationName = "Home atelier",
                    timeSlot = "09:00",
                    subtext = "3h - Home atelier",
                    cardColor = new Color(0.96f, 0.94f, 0.88f, 1f),
                    slotsCount = 1,
                    selectedRecipe = "Beras kencur",
                    selectedStation = "Home atelier",
                    goalType = PlannerGoalType.Craft,
                    skillBranch = PlannerSkillBranch.Moon,
                    accentColor = new Color(0.36f, 0.55f, 0.52f, 1f)
                };

            case PlannerGoalType.OpenShop:
                return new PlannerGoalCardData
                {
                    activityId = "open_shop",
                    activityName = "Open shop",
                    category = "Trade",
                    locationName = "Home and Shop",
                    timeSlot = "12:00",
                    subtext = "8h - Home and Shop",
                    cardColor = new Color(0.96f, 0.94f, 0.88f, 1f),
                    slotsCount = 1,
                    selectedRecipe = "Beras kencur",
                    selectedStation = "Home and Shop",
                    goalType = PlannerGoalType.OpenShop,
                    skillBranch = PlannerSkillBranch.Sun,
                    accentColor = new Color(0.83f, 0.65f, 0.27f, 1f)
                };

            case PlannerGoalType.MarketGuild:
                return new PlannerGoalCardData
                {
                    activityId = "market_guild",
                    activityName = "Market + guild",
                    category = "Trade",
                    locationName = "Market / Guild Hall",
                    timeSlot = "07:00",
                    subtext = "4h - Market and Guild Hall",
                    cardColor = new Color(0.96f, 0.94f, 0.88f, 1f),
                    slotsCount = 1,
                    selectedRecipe = "Beras kencur",
                    selectedStation = "Market",
                    goalType = PlannerGoalType.MarketGuild,
                    skillBranch = PlannerSkillBranch.Sun,
                    accentColor = new Color(0.55f, 0.45f, 0.68f, 1f)
                };

            case PlannerGoalType.FreeTime:
                return new PlannerGoalCardData
                {
                    activityId = "free_time",
                    activityName = "Free time",
                    category = "Rest",
                    locationName = "Home",
                    timeSlot = "18:00",
                    subtext = "4h or less - Home",
                    cardColor = new Color(0.96f, 0.94f, 0.88f, 1f),
                    slotsCount = 1,
                    selectedRecipe = "Beras kencur",
                    selectedStation = "Home",
                    goalType = PlannerGoalType.FreeTime,
                    skillBranch = PlannerSkillBranch.None,
                    accentColor = new Color(0.72f, 0.63f, 0.48f, 1f)
                };

            case PlannerGoalType.Custom:
                return new PlannerGoalCardData
                {
                    activityId = "custom",
                    activityName = "Custom goal",
                    category = "Planner",
                    locationName = "Home",
                    timeSlot = "09:00",
                    subtext = "Custom",
                    cardColor = new Color(0.96f, 0.94f, 0.88f, 1f),
                    slotsCount = 1,
                    selectedRecipe = "Beras kencur",
                    selectedStation = "Home atelier",
                    goalType = PlannerGoalType.Custom,
                    skillBranch = PlannerSkillBranch.None,
                    accentColor = new Color(0.65f, 0.58f, 0.47f, 1f)
                };

            default:
                return new PlannerGoalCardData
                {
                    activityId = "exploration",
                    activityName = "Explore",
                    category = "Fieldwork",
                    locationName = "City Outskirt",
                    timeSlot = "06:00",
                    subtext = "3h - City Outskirt",
                    cardColor = new Color(0.96f, 0.94f, 0.88f, 1f),
                    slotsCount = 1,
                    selectedRecipe = "Beras kencur",
                    selectedStation = "Gathering area",
                    goalType = PlannerGoalType.Explore,
                    skillBranch = PlannerSkillBranch.Earth,
                    accentColor = new Color(0.64f, 0.75f, 0.48f, 1f)
                };
        }
    }

    public static List<PlannerGoalCardData> GoalCards()
    {
        return new List<PlannerGoalCardData>
        {
            GetGoal(PlannerGoalType.Explore),
            GetGoal(PlannerGoalType.Craft),
            GetGoal(PlannerGoalType.OpenShop),
            GetGoal(PlannerGoalType.MarketGuild),
            GetGoal(PlannerGoalType.FreeTime)
        };
    }

    public static List<PlannerProductOffer> Products()
    {
        return new List<PlannerProductOffer>
        {
            new PlannerProductOffer
            {
                productId = "beras_kencur",
                displayName = "Beras kencur",
                demandMin = 4,
                demandMax = 6,
                usualPrice = 4,
                units = 0,
                ingredients = new List<PlannerIngredientNeed>
                {
                    Need("rice_grain", "Rice grain", 2),
                    Need("ginger_fresh", "Ginger fresh", 2),
                    Need("palm_sugar_block", "Palm sugar block", 1)
                }
            },
            new PlannerProductOffer
            {
                productId = "kunyit_asam",
                displayName = "Kunyit asam",
                demandMin = 4,
                demandMax = 6,
                usualPrice = 6,
                units = 12,
                ingredients = new List<PlannerIngredientNeed>
                {
                    Need("turmeric_fresh", "Turmeric fresh", 4),
                    Need("tamarind_paste", "Tamarind paste", 2),
                    Need("palm_sugar_block", "Palm sugar block", 2)
                }
            },
            new PlannerProductOffer
            {
                productId = "lemongrass_balm",
                displayName = "Lemongrass balm",
                demandMin = 9,
                demandMax = 15,
                usualPrice = 13,
                units = 0,
                ingredients = new List<PlannerIngredientNeed>
                {
                    Need("lemongrass_fresh", "Lemongrass fresh", 3),
                    Need("coconut_oil", "Coconut oil", 1),
                    Need("beeswax_block", "Beeswax block", 1)
                }
            },
            new PlannerProductOffer
            {
                productId = "turmeric_salep",
                displayName = "Turmeric salep",
                demandMin = 10,
                demandMax = 14,
                usualPrice = 12,
                units = 0,
                ingredients = new List<PlannerIngredientNeed>
                {
                    Need("turmeric_fresh", "Turmeric fresh", 3),
                    Need("coconut_oil", "Coconut oil", 1),
                    Need("beeswax_block", "Beeswax block", 1)
                }
            }
        };
    }

    public static List<PlannerMarketItem> MarketItems()
    {
        return new List<PlannerMarketItem>
        {
            Market("beras_kencur", "Beras kencur", true, 4f, 6f, 4f, PlannerMarketTrend.Down),
            Market("kunyit_asam", "Kunyit asam", true, 4.5f, 6.5f, 5.7f, PlannerMarketTrend.Up),
            Market("lemongrass_balm", "Lemongrass balm", true, 9f, 15f, 13f, PlannerMarketTrend.Up),
            Market("turmeric_salep", "Turmeric salep", true, 10f, 14f, 12f, PlannerMarketTrend.Up),

            Market("beeswax_block", "Beeswax block", false, 8f, 12f, 10.5f, PlannerMarketTrend.Stable),
            Market("coconut_oil", "Coconut oil", false, 5f, 8f, 7.9f, PlannerMarketTrend.Stable),
            Market("ginger_fresh", "Ginger fresh", false, 4f, 7f, 6.9f, PlannerMarketTrend.Up),
            Market("kencur_fresh", "Kencur fresh", false, 3f, 5f, 3.6f, PlannerMarketTrend.Stable),
            Market("lemongrass_fresh", "Lemongrass fresh", false, 2f, 4f, 2.9f, PlannerMarketTrend.Down),
            Market("palm_sugar_block", "Palm sugar block", false, 3f, 5f, 3.3f, PlannerMarketTrend.Stable),
            Market("rice_grain", "Rice grain", false, 1f, 5f, 1.8f, PlannerMarketTrend.Down),
            Market("tamarind_paste", "Tamarind paste", false, 3f, 5f, 3.5f, PlannerMarketTrend.Stable),
            Market("turmeric_fresh", "Turmeric fresh", false, 3f, 5f, 3.3f, PlannerMarketTrend.Up)
        };
    }

    public static List<PlannerInventoryItem> StartingInventory()
    {
        return new List<PlannerInventoryItem>
        {
            Item("turmeric_fresh", "Turmeric fresh", 4, false),
            Item("tamarind_paste", "Tamarind paste", 2, false),
            Item("palm_sugar_block", "Palm sugar block", 2, false),
            Item("coconut_oil", "Coconut oil", 1, false),
            Item("beeswax_block", "Beeswax block", 1, false),
            Item("kunyit_asam", "Kunyit asam", 6, true)
        };
    }

    public static List<PlannerScheduleEntry> Schedule()
    {
        return new List<PlannerScheduleEntry>
        {
            new PlannerScheduleEntry
            {
                locationName = "Home and Shop",
                openHour = 0,
                closeHour = 24,
                closedDay = -1,
                walkMinutes = 0
            },
            new PlannerScheduleEntry
            {
                locationName = "Market",
                openHour = 7,
                closeHour = 17,
                closedDay = 4,
                walkMinutes = 30
            },
            new PlannerScheduleEntry
            {
                locationName = "Guild Hall",
                openHour = 8,
                closeHour = 16,
                closedDay = 6,
                walkMinutes = 45
            },
            new PlannerScheduleEntry
            {
                locationName = "City Outskirt",
                openHour = 6,
                closeHour = 18,
                closedDay = -1,
                walkMinutes = 60
            }
        };
    }

    public static List<PlannerFirstStep> FirstSteps()
    {
        return new List<PlannerFirstStep>
        {
            Step("Give each day a goal", false),
            Step("Choose products to sell", true),
            Step("Share your gold into the three jars", true),
            Step("Buy herbs at the market or the guild", false),
            Step("Make your first product", false),
            Step("Dry fresh herbs on the rack", false),
            Step("Fill the shelf and open the shop", false),
            Step("Gather wild herbs at the City Outskirt", false),
            Step("Finish a commission for the town", false),
            Step("Pay at least the minimum at month end", false)
        };
    }

    private static PlannerIngredientNeed Need(string id, string name, int amount)
    {
        return new PlannerIngredientNeed
        {
            itemId = id,
            displayName = name,
            needed = amount
        };
    }

    private static PlannerMarketItem Market(string id, string name, bool product, float demandMin,
        float demandMax, float price, PlannerMarketTrend trend)
    {
        return new PlannerMarketItem
        {
            itemId = id,
            displayName = name,
            isProduct = product,
            demandMin = demandMin,
            demandMax = demandMax,
            usualPrice = price,
            nowPrice = price,
            trend = trend,
            priceBand = PlannerPriceBand.Fair
        };
    }

    private static PlannerInventoryItem Item(string id, string name, int amount, bool product)
    {
        return new PlannerInventoryItem
        {
            itemId = id,
            displayName = name,
            amount = amount,
            isProduct = product
        };
    }

    private static PlannerFirstStep Step(string text, bool done)
    {
        return new PlannerFirstStep
        {
            text = text,
            done = done
        };
    }
}
