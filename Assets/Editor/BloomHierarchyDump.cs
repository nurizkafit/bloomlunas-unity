using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BloomLunas.EditorTools
{
    /// <summary>
    /// Alat baca-saja: menulis struktur Hierarchy scene ke Logs/hierarchy_*.txt
    /// supaya wiring UI bisa dicocokkan dengan nama objek yang sudah ada.
    /// </summary>
    public static class BloomHierarchyDump
    {
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

        public static void Run()
        {
            Directory.CreateDirectory("Logs");
            foreach (string path in Scenes)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[Dump] missing scene " + path);
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var sb = new StringBuilder();
                sb.AppendLine("SCENE " + path);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Dump(root.transform, 0, sb);
                }

                string name = Path.GetFileNameWithoutExtension(path);
                File.WriteAllText("Logs/hierarchy_" + name + ".txt", sb.ToString());
                Debug.Log("[Dump] wrote Logs/hierarchy_" + name + ".txt");
            }
        }

        private static void Dump(Transform t, int depth, StringBuilder sb)
        {
            var rt = t as RectTransform;
            string extra = "";
            if (rt != null)
            {
                extra = " [rt " + rt.anchoredPosition.x.ToString("0") + "," + rt.anchoredPosition.y.ToString("0") +
                        " " + rt.sizeDelta.x.ToString("0") + "x" + rt.sizeDelta.y.ToString("0") + "]";
            }

            extra += " " + t.gameObject.activeSelf;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                string font = tmp.font != null ? tmp.font.name : "null";
                extra += " TMP('" + Trim(tmp.text) + "' " + tmp.fontSize.ToString("0.#") + " " + font + ")";
            }

            var image = t.GetComponent<Image>();
            if (image != null) extra += " Image";

            var button = t.GetComponent<Button>();
            if (button != null) extra += " Button";

            foreach (MonoBehaviour behaviour in t.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null) extra += " <" + behaviour.GetType().Name + ">";
            }

            sb.Append(new string(' ', depth * 2)).Append(t.name).Append(extra).AppendLine();

            for (int i = 0; i < t.childCount; i++)
            {
                Dump(t.GetChild(i), depth + 1, sb);
            }
        }

        private static string Trim(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace("\n", " ").Replace("\r", "");
            return value.Length > 40 ? value.Substring(0, 40) + "..." : value;
        }
    }
}
