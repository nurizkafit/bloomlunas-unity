using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
                SetupCityHud();
                if (path.EndsWith("MulaiGame.unity")) SetupReviewModal();
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

        private static void ApplyFont(TMP_Text text, bool forceHeading = false)
        {
            if (text == null) return;

            bool heading = forceHeading ||
                           text.fontSize >= 30f ||
                           text.name == "Title" ||
                           text.name.StartsWith("Title") ||
                           text.name == "PageTitle" ||
                           text.name == "Header";

            TMP_FontAsset font = heading ? headingFont : bodyFont;
            if (font != null) text.font = font;
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
