using UnityEngine;
using UnityEngine.UI;

public class PlannerBookManager : MonoBehaviour
{
    [Header("Halaman Buku")]
    [SerializeField] protected GameObject goalsPage;
    [SerializeField] protected GameObject storePage;
    [SerializeField] protected GameObject ledgerPage;
    [SerializeField] protected GameObject marketPage;
    [SerializeField] protected GameObject hoursPage;
    [SerializeField] protected GameObject skillPage;
    [SerializeField] protected GameObject notesPage;
    [SerializeField] protected GameObject questPage;

    public virtual void SwitchPage(string pageName)
    {
        // Matikan semua halaman terlebih dahulu
        if (goalsPage != null) goalsPage.SetActive(false);
        if (storePage != null) storePage.SetActive(false);
        if (ledgerPage != null) ledgerPage.SetActive(false);
        if (marketPage != null) marketPage.SetActive(false);
        if (hoursPage != null) hoursPage.SetActive(false);
        if (skillPage != null) skillPage.SetActive(false);
        if (notesPage != null) notesPage.SetActive(false);
        if (questPage != null) questPage.SetActive(false);

        // Nyalakan halaman yang dipilih
        switch (pageName)
        {
            case "Goals": if (goalsPage != null) goalsPage.SetActive(true); break;
            case "Store": if (storePage != null) storePage.SetActive(true); break;
            case "Ledger": if (ledgerPage != null) ledgerPage.SetActive(true); break;
            case "Market": if (marketPage != null) marketPage.SetActive(true); break;
            case "Hours": if (hoursPage != null) hoursPage.SetActive(true); break;
            case "Skill": if (skillPage != null) skillPage.SetActive(true); break;
            case "Notes": if (notesPage != null) notesPage.SetActive(true); break;
            case "Quest": if (questPage != null) questPage.SetActive(true); break;
        }
    }
}