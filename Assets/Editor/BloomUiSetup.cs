using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BloomLunas.EditorTools
{
    /// <summary>
    /// Satu kali jalan: pasang font Redaction/Xmind, tinggikan Card_Prefab,
    /// sambungkan ReviewModal di MulaiGame, dan pasang CityHudUI ke semua
    /// scene kota. Hasilnya disimpan ke scene/prefab, jadi tidak perlu script
    /// ini saat Play.
    /// </summary>
    public static class BloomUiSetup
    {
        private static TMP_FontAsset headingFont;
        private static TMP_FontAsset bodyFont;

        private static readonly string[] Scenes =
        {
            "Assets/Scenes/MulaiGame.unity",
            "Assets/Scenes/CityOverWorld.unity",
            "Assets/Scenes/ShopdanRumah.unity",
            "Assets/Scenes/Market.unity",
            "Assets/Scenes/Guild.unity",
            "Assets/Scenes/Bedroom.unity",
            "Assets/Scenes/CraftingRoom.unity",
            "Assets/Scenes/MainMenu.unity"
        };

        [MenuItem("Tools/Bloom Lunas/Setup UI")]
        public static void Run()
        {
            headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Redaction/Redaction-Regular SDF.asset");
            bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Xmind/Xmind-VariableFont_wght SDF.asset");
            if (headingFont == null) Debug.LogWarning("[Setup] Redaction font not found");
            if (bodyFont == null) Debug.LogWarning("[Setup] Xmind font not found");

            FixCardPrefab();

            foreach (string path in Scenes)
            {
                if (!System.IO.File.Exists(path)) continue;

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                SetupEventSystem();
                SetupCityHud();
                SetupCityExtras(scene);
                if (path.EndsWith("MulaiGame.unity"))
                {
                    SetupReviewModal();
                    ImprovePlannerReadability();
                }
                ApplyFonts(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[Setup] saved " + path);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] done");
        }

        // -----------------------------------------------------------------
        // Kartu
        // -----------------------------------------------------------------

        private static void FixCardPrefab()
        {
            const string path = "Assets/Card_Prefab.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null) return;

            RectTransform root = contents.transform as RectTransform;
            if (root != null && root.sizeDelta.y < 95f)
            {
                root.sizeDelta = new Vector2(root.sizeDelta.x, 100f);
            }

            foreach (TMP_Text text in contents.GetComponentsInChildren<TMP_Text>(true))
            {
                ApplyFont(text);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log("[Setup] card prefab updated");
        }

        // -----------------------------------------------------------------
        // ReviewModal di MulaiGame
        // -----------------------------------------------------------------

        private static void SetupReviewModal()
        {
            Transform canvas = FindRoot("Canvas");
            if (canvas == null) return;

            Transform modal = Find(canvas, "PlannerBookUI/ReviewModal");
            if (modal == null)
            {
                // Nama jalur bisa berbeda; cari berdasarkan nama.
                modal = FindByName(canvas, "ReviewModal");
            }
            if (modal == null)
            {
                Debug.LogWarning("[Setup] ReviewModal not found");
                return;
            }

            Transform viewport = Find(modal, "Panel/SummaryViewport");
            if (viewport == null)
            {
                Debug.LogWarning("[Setup] SummaryViewport not found");
                return;
            }

            // Sembunyikan teks ringkasan lama supaya tidak menumpuk baris baru.
            Transform oldSummary = Find(viewport, "Summary");
            if (oldSummary != null) oldSummary.gameObject.SetActive(false);

            string[] labels = { "Sales goal", "Herbs", "Living costs", "Sales (a guess)", "Gold after" };
            Sprite dividerSprite = FindDividerSprite(modal);
            var values = new TMP_Text[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                string rowName = "Row_" + labels[i].Replace(" ", "").Replace("(", "").Replace(")", "");
                Transform existing = Find(viewport, rowName);
                RectTransform row = existing != null
                    ? existing as RectTransform
                    : CreateRow(viewport, rowName, i, dividerSprite);

                TMP_Text labelText = Find(row, "Label")?.GetComponent<TMP_Text>();
                TMP_Text valueText = Find(row, "Value")?.GetComponent<TMP_Text>();
                if (labelText != null) labelText.text = labels[i];
                if (valueText != null) valueText.text = "-";

                ApplyFont(labelText, false);
                ApplyFont(valueText, true);
                values[i] = valueText;
            }

            PlannerBookUI planner = FindComponent<PlannerBookUI>(Find(canvas, "PlannerBookUI"));
            if (planner != null)
            {
                Set(planner, "reviewSalesGoalLabel", values[0]);
                Set(planner, "reviewHerbsLabel", values[1]);
                Set(planner, "reviewLivingLabel", values[2]);
                Set(planner, "reviewSalesGuessLabel", values[3]);
                Set(planner, "reviewGoldAfterLabel", values[4]);
                Debug.Log("[Setup] review summary labels assigned");
            }
            else
            {
                Debug.LogWarning("[Setup] PlannerBookUI component not found");
            }

            SetupDeck(planner);
        }

        /// <summary>
        /// MulaiGame: naikkan teks yang terlalu kecil supaya halaman buku
        /// tetap terbaca di layar 1080p/1440p.
        /// </summary>
        private static void ImprovePlannerReadability()
        {
            Transform canvas = FindRoot("Canvas");
            if (canvas == null) return;
            Transform book = Find(canvas, "PlannerBookUI");
            if (book == null) book = canvas;

            foreach (TMP_Text text in book.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "PageNumber") continue;

                float min = 18f;
                if (text.name == "Body") min = 20f;

                if (text.fontSize < min) text.fontSize = min;
            }

            Debug.Log("[Setup] planner text sizes raised");
        }

        private static void SetupDeck(PlannerBookUI planner)
        {
            Transform goals = null;
            Transform canvas = FindRoot("Canvas");
            if (canvas != null) goals = FindByName(canvas, "CardsContainer");
            if (goals == null) return;

            Transform viewport = goals.parent;
            if (viewport != null && viewport.GetComponent<DeckDropArea>() == null)
            {
                DeckDropArea area = viewport.gameObject.AddComponent<DeckDropArea>();
                Set(area, "planner", planner);
            }

            // Area drop harus menerima raycast supaya kartu bisa dikembalikan ke dek.
            Image viewportImage = viewport != null ? viewport.GetComponent<Image>() : null;
            if (viewportImage != null) viewportImage.raycastTarget = true;

            foreach (DraggableCard card in goals.GetComponentsInChildren<DraggableCard>(true))
            {
                SetBool(card, "isDeckTemplate", true);
            }

            // Satu kartu slot untuk tiap kotak hari, sudah ada di scene (tidak
            // di-Instantiate saat Play), posisinya mengikuti kotak hari.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Card_Prefab.prefab");
            if (prefab != null && canvas != null)
            {
                foreach (DropSlot slot in canvas.GetComponentsInChildren<DropSlot>(true))
                {
                    if (slot.GetComponentInChildren<DraggableCard>(true) != null) continue;

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, slot.transform);
                    instance.name = "SlotCard";
                    RectTransform rect = instance.transform as RectTransform;
                    if (rect != null)
                    {
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = Vector2.zero;
                        rect.offsetMax = Vector2.zero;
                    }

                    DraggableCard card = instance.GetComponent<DraggableCard>();
                    if (card != null)
                    {
                        SetBool(card, "isDeckTemplate", false);
                        SetBool(card, "applyPresetOnAwake", false);
                    }
                    instance.SetActive(false);
                }
            }

            Debug.Log("[Setup] deck drop area ready (" + goals.childCount + " cards)");
        }

        private static Sprite FindDividerSprite(Transform modal)
        {
            foreach (Image image in modal.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "Divider") return image.sprite;
            }
            return null;
        }

        private static RectTransform CreateRow(Transform parent, string name, int index, Sprite divider)
        {
            GameObject rowObject = new GameObject(name, typeof(RectTransform));
            RectTransform row = rowObject.GetComponent<RectTransform>();
            row.SetParent(parent, false);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -6f - index * 52f);
            row.sizeDelta = new Vector2(-8f, 46f);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(row, false);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = new Vector2(-140f, 0f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Label";
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.24f, 0.33f, 0.29f, 1f);
            label.raycastTarget = false;

            GameObject valueObject = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform valueRect = valueObject.GetComponent<RectTransform>();
            valueRect.SetParent(row, false);
            valueRect.anchorMin = new Vector2(1f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = new Vector2(-8f, 0f);
            valueRect.sizeDelta = new Vector2(150f, 0f);
            TextMeshProUGUI value = valueObject.GetComponent<TextMeshProUGUI>();
            value.text = "-";
            value.fontSize = 24f;
            value.alignment = TextAlignmentOptions.MidlineRight;
            value.color = new Color(0.24f, 0.33f, 0.29f, 1f);
            value.raycastTarget = false;

            if (divider != null)
            {
                GameObject lineObject = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform lineRect = lineObject.GetComponent<RectTransform>();
                lineRect.SetParent(row, false);
                lineRect.anchorMin = new Vector2(0f, 0f);
                lineRect.anchorMax = new Vector2(1f, 0f);
                lineRect.pivot = new Vector2(0.5f, 0f);
                lineRect.offsetMin = new Vector2(8f, 0f);
                lineRect.offsetMax = new Vector2(-8f, 2f);
                Image line = lineObject.GetComponent<Image>();
                line.sprite = divider;
                line.type = Image.Type.Sliced;
                line.raycastTarget = false;
            }

            return row;
        }

        // -----------------------------------------------------------------
        // UI kota di semua scene
        // -----------------------------------------------------------------

        private static void SetupCityHud()
        {
            Transform canvas = FindRoot("Canvas");
            if (canvas == null) return;

            CityWorldUI old = canvas.GetComponent<CityWorldUI>();
            if (old != null)
            {
                Object.DestroyImmediate(old);
                Debug.Log("[Setup] removed runtime CityWorldUI from " + SceneManager.GetActiveScene().name);
            }

            CityHudUI hud = canvas.GetComponent<CityHudUI>();
            if (hud == null) hud = canvas.gameObject.AddComponent<CityHudUI>();

            Set(hud, "goldValue", Text(canvas, "Header/Stat_GOLD/Value"));
            Set(hud, "debtValue", Text(canvas, "Header/Stat_DEBT/Value"));
            Set(hud, "calendarValue", Text(canvas, "Header/Stat_CALENDAR/Value"));
            Set(hud, "mealpackValue", Text(canvas, "Header/Stat_MEALPACK/Value"));

            Transform eventPanel = Find(canvas, "PanelEventyangudahdipilih");
            Set(hud, "eventTime", Text(eventPanel, "Jam"));
            Set(hud, "eventTitle", Text(eventPanel, "namaTempat"));

            Transform placePanel = Find(canvas, "PanelSaatKitaKlikGedung");
            if (placePanel != null)
            {
                TMP_Text title = Text(placePanel, "PlaceName");
                if (title == null) title = CreatePlaceText(placePanel, "PlaceName", -20f, 34f, true);
                TMP_Text detail = Text(placePanel, "PlaceDetail");
                if (detail == null) detail = CreatePlaceText(placePanel, "PlaceDetail", -62f, 18f, false);

                Set(hud, "placePanel", placePanel.gameObject);
                Set(hud, "placeTitle", title);
                Set(hud, "placeDetail", detail);
                Set(hud, "goButton", Button(placePanel, "ButtonGo"));
                placePanel.gameObject.SetActive(false);
            }

            Transform list = Find(canvas, "PanelBuatMilihTempat");
            if (list != null)
            {
                Set(hud, "placeListPanel", list.gameObject);
                Set(hud, "homeButton", Button(list, "ButtonHome and Shop"));
                Set(hud, "marketButton", Button(list, "ButtonMarket"));
                Set(hud, "guildButton", Button(list, "ButtonGuildhall"));
                Set(hud, "outskirtButton", Button(list, "ButtonCityOutskirt"));
                Set(hud, "wholeTownButton", Button(list, "ButtonSeeTheWholeTown"));
            }

            Set(hud, "townButton", Button(canvas, "ButtonTown"));
            Set(hud, "journalButton", Button(canvas, "ButtonJournal"));
            Set(hud, "shortcutTownButton", Button(canvas, "PanelMarket,dll/ButtonTown"));
            Set(hud, "shortcutHomeButton", Button(canvas, "PanelMarket,dll/ButtonHome"));

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "MainMenu" && sceneName != "MulaiGame")
            {
                Button endDay = Button(canvas, "ButtonEndDay");
                if (endDay == null) endDay = CreateEndDayButton(canvas);
                Set(hud, "endDayButton", endDay);
            }

            CityCameraController camera = Object.FindObjectOfType<CityCameraController>(true);
            if (camera != null) Set(camera, "cityHud", hud);

            Debug.Log("[Setup] CityHudUI wired in " + SceneManager.GetActiveScene().name);
        }

        // -----------------------------------------------------------------
        // City Outskirt & Market stall
        // -----------------------------------------------------------------

        private static readonly string[,] SpotData =
        {
            { "beeswax_block", "Wild bee hive", "2", "90" },
            { "ginger_fresh", "Ginger under the palms", "2", "60" },
            { "kencur_fresh", "Kencur bed by the stream", "2", "60" },
            { "lemongrass_fresh", "Lemongrass field", "2", "45" },
            { "palm_sugar_block", "Old herb garden", "2", "75" },
            { "tamarind_paste", "Tamarind tree", "2", "45" },
            { "turmeric_fresh", "Wild turmeric patch", "2", "60" }
        };

        private static readonly string[,] BuyData =
        {
            { "beeswax_block", "Beeswax block" },
            { "coconut_oil", "Coconut oil" },
            { "ginger_fresh", "Ginger fresh" },
            { "kencur_fresh", "Kencur fresh" },
            { "lemongrass_fresh", "Lemongrass fresh" },
            { "palm_sugar_block", "Palm sugar block" },
            { "rice_grain", "Rice grain" },
            { "tamarind_paste", "Tamarind paste" },
            { "turmeric_fresh", "Turmeric fresh" }
        };

        private static readonly string[,] SellData =
        {
            { "beras_kencur", "Beras kencur" },
            { "kunyit_asam", "Kunyit asam" },
            { "lemongrass_balm", "Lemongrass balm" },
            { "turmeric_salep", "Turmeric salep" }
        };

        /// <summary>
        /// Scene kota (Market, Guild, Home, Bedroom, Crafting) belum punya
        /// EventSystem, jadi semua tombol UI tidak bisa diklik. Buat satu per scene.
        /// </summary>
        private static void SetupEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>(true) != null) return;

            GameObject system = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            InputSystemUIInputModule module = system.GetComponent<InputSystemUIInputModule>();
            if (module != null) module.AssignDefaultActions();
            Debug.Log("[Setup] EventSystem created in " + SceneManager.GetActiveScene().name);
        }

        private static void SetupCityExtras(Scene scene)
        {
            if (scene.name == "CityOverWorld") SetupOutskirt();
            else if (scene.name == "Market") SetupMarketStall();
        }

        private static void SetupOutskirt()
        {
            Transform canvas = FindRoot("Canvas");
            if (canvas == null) return;

            Transform confirm = Find(canvas, "PanelSebelumConfirmexplore");
            Transform gameplay = Find(confirm, "PanelCityOutskirt");
            Transform found = Find(gameplay, "PanelYouFoundaSpot");
            if (confirm == null || gameplay == null || found == null)
            {
                Debug.LogWarning("[Setup] outskirt panels not found");
                return;
            }

            Transform herbParent = Find(gameplay, "PanelHerbsOutHere");
            Transform basketParent = Find(gameplay, "PanelInyourBasket");

            Transform herbRows = EnsureRows(herbParent, "Rows", 52f, 34f);
            GatherSpotRow[] spots = new GatherSpotRow[SpotData.GetLength(0)];
            for (int i = 0; i < spots.Length; i++)
            {
                string rowName = "Spot_" + SpotData[i, 1].Replace(" ", string.Empty);
                GameObject row = new GameObject(rowName, typeof(RectTransform), typeof(GatherSpotRow));
                RectTransform rect = row.GetComponent<RectTransform>();
                rect.SetParent(herbRows, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, 34f);
                rect.anchoredPosition = new Vector2(0f, -i * 34f);

                TMP_Text label = MakeText(row.transform, "Name", SpotData[i, 1], 20f,
                    TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(1f, 1f),
                    new Vector2(6f, 0f), new Vector2(-70f, 0f));
                TMP_Text time = MakeText(row.transform, "Time", "", 18f,
                    TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(1f, 1f),
                    new Vector2(-64f, 0f), new Vector2(-6f, 0f));

                GatherSpotRow view = row.GetComponent<GatherSpotRow>();
                Set(view, "nameLabel", label);
                Set(view, "timeLabel", time);
                SetString(view, "itemId", SpotData[i, 0]);
                SetString(view, "itemName", SpotData[i, 1]);
                SetInt(view, "amount", int.Parse(SpotData[i, 2]));
                SetInt(view, "minutes", int.Parse(SpotData[i, 3]));
                if (time != null) time.text = view.TimeText;
                spots[i] = view;
            }

            Transform basketRows = EnsureRows(basketParent, "Rows", 52f, 30f);
            BasketRowView[] rows = new BasketRowView[spots.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                GameObject row = new GameObject("Basket_" + SpotData[i, 0], typeof(RectTransform), typeof(BasketRowView));
                RectTransform rect = row.GetComponent<RectTransform>();
                rect.SetParent(basketRows, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, 30f);
                rect.anchoredPosition = new Vector2(0f, -i * 30f);

                TMP_Text label = MakeText(row.transform, "Name", SpotData[i, 1], 18f,
                    TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one,
                    new Vector2(6f, 0f), new Vector2(-50f, 0f));
                TMP_Text count = MakeText(row.transform, "Count", "x0", 18f,
                    TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(1f, 1f),
                    new Vector2(-46f, 0f), new Vector2(-6f, 0f));

                BasketRowView view = row.GetComponent<BasketRowView>();
                Set(view, "nameLabel", label);
                Set(view, "countLabel", count);
                SetString(view, "itemId", SpotData[i, 0]);
                SetString(view, "itemName", SpotData[i, 1]);
                row.SetActive(false);
                rows[i] = view;
            }

            TMP_Text basketCount = Find(basketParent, "BasketCount")?.GetComponent<TMP_Text>();
            if (basketCount == null)
            {
                basketCount = MakeText(basketParent, "BasketCount", "0/5", 18f,
                    TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-80f, -38f), new Vector2(-10f, -12f));
            }

            TMP_Text amount = Find(found, "Amount")?.GetComponent<TMP_Text>();
            if (amount == null)
            {
                amount = MakeText(found, "Amount", "", 16f, TextAlignmentOptions.TopLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -86f), new Vector2(-18f, -58f));
            }

            CityExploreUI explore = canvas.GetComponent<CityExploreUI>();
            if (explore == null) explore = canvas.gameObject.AddComponent<CityExploreUI>();

            Set(explore, "hud", canvas.GetComponent<CityHudUI>());
            Set(explore, "cameraRig", Object.FindObjectOfType<CityCameraController>(true));
            Set(explore, "hubEventPanel", Find(canvas, "PanelEventyangudahdipilih")?.gameObject);
            Set(explore, "hubPlaceListPanel", Find(canvas, "PanelBuatMilihTempat")?.gameObject);
            Set(explore, "confirmPanel", confirm.gameObject);
            Set(explore, "gameplayPanel", gameplay.gameObject);
            Set(explore, "foundPanel", found.gameObject);
            Set(explore, "exploreButton", Button(confirm, "PanelBuatMilihTempat (1)/ButtonExploretheOutskirt"));
            Set(explore, "keepWalkingButton", Button(confirm, "ButtonKeepWalking"));
            Set(explore, "headBackButton", Button(confirm, "ButtonHeadBack"));
            Set(explore, "townButton", Button(confirm, "PanelBuatMilihTempat (1)/ButtonTown"));
            Set(explore, "goHomeButton", Button(confirm, "PanelBuatMilihTempat (1)/ButtonGoHome"));
            Set(explore, "gatherButton", Button(found, "ButtonGather"));
            Set(explore, "passByButton", Button(found, "ButtonPassby"));
            Set(explore, "basketCountLabel", basketCount);
            Set(explore, "foundName", Text(found, "You found a spot"));
            Set(explore, "foundTime", Text(found, "itemyangsayadapat"));
            Set(explore, "foundAmount", amount);
            SetArray(explore, "spots", spots);
            SetArray(explore, "basketRows", rows);

            CityHudUI hud = canvas.GetComponent<CityHudUI>();
            if (hud != null)
            {
                Set(hud, "outskirtUI", explore);
                Set(hud, "eventPanel", Find(canvas, "PanelEventyangudahdipilih")?.gameObject);
                Set(hud, "cameraRig", Object.FindObjectOfType<CityCameraController>(true));
                Set(hud, "player", Object.FindObjectOfType<CityPlayerController>(true));
            }

            gameplay.gameObject.SetActive(false);
            found.gameObject.SetActive(false);
            confirm.gameObject.SetActive(false);
            Debug.Log("[Setup] outskirt UI wired (" + spots.Length + " spots)");
        }

        private static void SetupMarketStall()
        {
            Transform canvas = FindRoot("Canvas");
            if (canvas == null) return;

            Transform stall = Find(canvas, "PanelMarketstall");
            Transform buy = Find(stall, "PanelHerbofMaterial");
            Transform food = Find(stall, "PanelFood");
            Transform sell = Find(stall, "PanelSell");
            if (stall == null || buy == null || food == null || sell == null)
            {
                Debug.LogWarning("[Setup] market stall panels not found");
                return;
            }

            // Tombol -/+ tunggal lama diganti tombol per baris.
            Transform oldMinus = Find(buy, "Button-");
            Transform oldPlus = Find(buy, "Button+");
            if (oldMinus != null) oldMinus.gameObject.SetActive(false);
            if (oldPlus != null) oldPlus.gameObject.SetActive(false);

            Transform buyRows = EnsureRows(buy, "Rows", 58f, 32f);
            MarketRowView[] buys = new MarketRowView[BuyData.GetLength(0)];
            for (int i = 0; i < buys.Length; i++)
            {
                buys[i] = MakeMarketRow(buyRows, "Buy_" + BuyData[i, 0], BuyData[i, 0], BuyData[i, 1], false, i);
            }

            Transform sellRows = EnsureRows(sell, "Rows", 58f, 32f);
            MarketRowView[] sells = new MarketRowView[SellData.GetLength(0)];
            for (int i = 0; i < sells.Length; i++)
            {
                sells[i] = MakeMarketRow(sellRows, "Sell_" + SellData[i, 0], SellData[i, 0], SellData[i, 1], true, i);
            }

            TMP_Text storeLeft = Find(buy, "StoreLeft")?.GetComponent<TMP_Text>();
            if (storeLeft == null)
            {
                storeLeft = MakeText(buy, "StoreLeft", "Store jar left", 16f, TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -34f), new Vector2(-16f, -10f));
            }

            TMP_Text personalLeft = Find(food, "PersonalLeft")?.GetComponent<TMP_Text>();
            if (personalLeft == null)
            {
                personalLeft = MakeText(food, "PersonalLeft", "Personal jar left", 16f, TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 10f), new Vector2(-16f, 34f));
            }

            Button close = Button(stall, "ButtonClose");
            if (close == null) close = CreateCloseButton(stall);

            MarketStallUI ui = canvas.GetComponent<MarketStallUI>();
            if (ui == null) ui = canvas.gameObject.AddComponent<MarketStallUI>();

            Set(ui, "stallPanel", stall.gameObject);
            Set(ui, "openButton", Button(canvas, "PanelMarket,dll/ButtonMarketStall"));
            Set(ui, "closeButton", close);
            Set(ui, "tabBuy", Button(stall, "ButtonBuy"));
            Set(ui, "tabFood", Button(stall, "ButtonFood"));
            Set(ui, "tabSell", Button(stall, "ButtonSell"));
            Set(ui, "buyPanel", buy.gameObject);
            Set(ui, "foodPanel", food.gameObject);
            Set(ui, "sellPanel", sell.gameObject);
            Set(ui, "buyTotal", Text(buy, "berapabanyakuangygakandiperukan/Text (TMP)"));
            Set(ui, "storeLeft", storeLeft);
            Set(ui, "buyButton", Button(buy, "ButtonSell"));
            Set(ui, "foodPlus", Button(food, "Button+"));
            Set(ui, "foodMinus", Button(food, "Button-"));
            Set(ui, "foodQty", Text(food, "PanelBerapabanyakaygdiberli/Berapabanyakaygdiberli"));
            Set(ui, "foodTotal", Text(food, "butuhberapauang/Text (TMP)"));
            Set(ui, "foodDays", Text(food, "PanelMealPack/HowMuch(DaysCovered)"));
            Set(ui, "foodPrice", Text(food, "PanelMealPack/Price Each (1)"));
            Set(ui, "mealpackHave", Text(food, "PanelMealPack/HowMuch(YouHave)"));
            Set(ui, "personalLeft", personalLeft);
            Set(ui, "foodBuyButton", Button(food, "ButtonBuyFood"));
            Set(ui, "sellTotal", Text(sell, "berapabanyakygkitadapat/Text (TMP)"));
            Set(ui, "sellButton", Button(sell, "ButtonSell"));
            Set(ui, "selectAllButton", Button(sell, "ButtonSelectAll"));
            Set(ui, "deselectAllButton", Button(sell, "ButtonDeselectAll"));
            SetArray(ui, "buyRows", buys);
            SetArray(ui, "sellRows", sells);

            buy.gameObject.SetActive(false);
            food.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            stall.gameObject.SetActive(false);
            Debug.Log("[Setup] market stall wired (" + buys.Length + " buy, " + sells.Length + " sell)");
        }

        private static MarketRowView MakeMarketRow(Transform parent, string name, string itemId, string label,
            bool product, int index)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(MarketRowView));
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 32f);
            rect.anchoredPosition = new Vector2(0f, -index * 32f);

            TMP_Text nameText = MakeText(row.transform, "Name", label, 18f,
                TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one,
                new Vector2(6f, 0f), new Vector2(-204f, 0f));
            TMP_Text eachText = MakeText(row.transform, "Each", "-", 18f,
                TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-200f, 0f), new Vector2(-150f, 0f));
            TMP_Text haveText = MakeText(row.transform, "Have", "0", 18f,
                TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-146f, 0f), new Vector2(-106f, 0f));
            TMP_Text qtyText = MakeText(row.transform, "Qty", "0", 18f,
                TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-102f, 0f), new Vector2(-66f, 0f));

            Button minus = MakeSmallButton(row.transform, "Minus", "-", -62f);
            Button plus = MakeSmallButton(row.transform, "Plus", "+", -30f);

            MarketRowView view = row.GetComponent<MarketRowView>();
            Set(view, "nameLabel", nameText);
            Set(view, "eachLabel", eachText);
            Set(view, "haveLabel", haveText);
            Set(view, "qtyLabel", qtyText);
            Set(view, "plusButton", plus);
            Set(view, "minusButton", minus);
            SetString(view, "itemId", itemId);
            SetString(view, "itemName", label);
            SetBool(view, "isProduct", product);
            view.SetQty(0);
            return view;
        }

        private static Button MakeSmallButton(Transform parent, string name, string label, float x)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(28f, 28f);
            rect.anchoredPosition = new Vector2(x, 0f);

            Image image = obj.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.9f);

            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = MakeText(rect, "Label", label, 20f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.raycastTarget = false;
            return button;
        }

        private static Button CreateCloseButton(Transform stall)
        {
            Button template = Button(stall, "ButtonBuy");
            GameObject obj = new GameObject("ButtonClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(stall, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(46f, 46f);
            rect.anchoredPosition = new Vector2(-18f, -18f);

            Image image = obj.GetComponent<Image>();
            if (template != null && template.targetGraphic is Image templateImage)
            {
                image.sprite = templateImage.sprite;
                image.type = templateImage.type;
                image.color = templateImage.color;
            }

            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            TMP_Text text = MakeText(rect, "Label", "X", 22f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.raycastTarget = false;
            return button;
        }

        private static Transform EnsureRows(Transform parent, string name, float topOffset, float rowHeight)
        {
            if (parent == null) return null;

            Transform existing = Find(parent, name);
            if (existing != null)
            {
                for (int i = existing.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(existing.GetChild(i).gameObject);
                }
                return existing;
            }

            GameObject rows = new GameObject(name, typeof(RectTransform));
            RectTransform rect = rows.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(16f, 12f);
            rect.offsetMax = new Vector2(-16f, -topOffset);
            return rows.transform;
        }

        private static TMP_Text MakeText(Transform parent, string name, string value, float size,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.24f, 0.24f, 0.2f, 1f);
            text.raycastTarget = false;
            ApplyFont(text);
            return text;
        }

        private static Button CreateEndDayButton(Transform canvas)
        {
            Sprite sprite = null;
            Image template = null;
            Transform go = Find(canvas, "PanelSaatKitaKlikGedung/ButtonGo");
            if (go != null) template = go.GetComponent<Image>();
            if (template != null) sprite = template.sprite;

            GameObject obj = new GameObject("ButtonEndDay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(canvas, false);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 36f);
            rect.sizeDelta = new Vector2(230f, 70f);

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.type = template != null ? template.type : Image.Type.Sliced;
            image.color = template != null ? template.color : Color.white;

            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "End day";
            label.fontSize = 26f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.24f, 0.24f, 0.2f, 1f);
            label.raycastTarget = false;
            if (bodyFont != null) label.font = bodyFont;

            return button;
        }

        private static TMP_Text CreatePlaceText(Transform parent, string name, float y, float size, bool heading)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(26f, y);
            rect.sizeDelta = new Vector2(300f, heading ? 42f : 46f);

            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.text = name;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = new Color(0.24f, 0.24f, 0.2f, 1f);
            text.raycastTarget = false;
            text.font = heading && headingFont != null ? headingFont : bodyFont;
            return text;
        }

        // -----------------------------------------------------------------
        // Font
        // -----------------------------------------------------------------

        private static void ApplyFonts(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    ApplyFont(text);
                }
            }
        }

        /// <summary>Semua teks di semua scene memakai Xmind.</summary>
        private static void ApplyFont(TMP_Text text, bool forceHeading = false)
        {
            if (text == null) return;
            if (bodyFont != null) text.font = bodyFont;
        }

        // -----------------------------------------------------------------
        // Helper
        // -----------------------------------------------------------------

        private static Transform FindRoot(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name) return root.transform;
            }
            return null;
        }

        private static Transform Find(Transform root, string path)
        {
            return root != null ? root.Find(path) : null;
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindByName(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static TMP_Text Text(Transform root, string path)
        {
            Transform found = Find(root, path);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private static Button Button(Transform root, string path)
        {
            Transform found = Find(root, path);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static T FindComponent<T>(Transform root) where T : Component
        {
            return root != null ? root.GetComponent<T>() : null;
        }

        private static void Set(Component target, string field, Object value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Setup] field " + field + " not found on " + target.GetType().Name);
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Component target, string field, string value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) return;
            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Component target, string field, int value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) return;
            property.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Component target, string field, Object[] values)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Setup] array field " + field + " not found on " + target.GetType().Name);
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Component target, string field, bool value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) return;
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
