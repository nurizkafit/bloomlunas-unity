using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tombol di Main Menu. "Play" memulai permainan di scene planner,
/// "Quit" menutup game. Listener lama di scene menunjuk ke objek yang
/// sudah tidak ada, jadi aksi dipasang ulang dari script ini.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string firstScene = "MulaiGame";

    private bool wired;

    private void Awake()
    {
        Wire();
    }

    private void Wire()
    {
        if (wired) return;
        wired = true;

        if (playButton != null) playButton.onClick.AddListener(Play);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
    }

    public void Play()
    {
        if (string.IsNullOrEmpty(firstScene)) return;
        SceneManager.LoadScene(firstScene);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
