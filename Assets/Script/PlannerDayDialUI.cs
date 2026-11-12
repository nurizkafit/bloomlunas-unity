using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Day dial HUD yang dibentuk dari komponen UI terpisah:
/// sky disc, ring dengan Image Type Filled, dan ikon matahari/bulan.
/// Script ini tidak membuat objek; semua komponen diisi di Inspector.
/// </summary>
public class PlannerDayDialUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlannerBook book;
    [SerializeField] private CoreLoop coreLoop;

    [Header("Komponen Dial")]
    [SerializeField] private Image skyDisc;
    [SerializeField] private Image timeRing;
    [SerializeField] private Image energyRing;
    [SerializeField] private RectTransform sunMarker;
    [SerializeField] private RectTransform moonMarker;
    [SerializeField] private TMP_Text weekdayText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text periodText;
    [SerializeField] private TMP_Text energyText;

    [Header("Waktu")]
    [Range(0, 23)] [SerializeField] private int hour = 6;
    [Range(0, 59)] [SerializeField] private int minute = 0;
    [Tooltip("Jarak ikon matahari/bulan dari pusat dial.")]
    [SerializeField] private float markerRadius = 50f;

    [Header("Warna Langit")]
    [SerializeField] private Color nightSky = new Color(0.12f, 0.16f, 0.27f, 1f);
    [SerializeField] private Color dawnSky = new Color(0.95f, 0.84f, 0.64f, 1f);
    [SerializeField] private Color daySky = new Color(0.83f, 0.89f, 0.85f, 1f);
    [SerializeField] private Color duskSky = new Color(0.93f, 0.70f, 0.49f, 1f);
    [SerializeField] private Color lowEnergyColor = new Color(0.65f, 0.28f, 0.22f, 1f);
    [SerializeField] private Color goodEnergyColor = new Color(0.45f, 0.65f, 0.32f, 1f);

    private bool subscribed;

    private void Awake()
    {
        Resolve();
        ResolveParts();
    }

    private void OnEnable()
    {
        Resolve();
        ResolveParts();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Resolve()
    {
        if (book == null) book = PlannerBook.Instance;
        if (coreLoop == null) coreLoop = CoreLoop.Instance;
    }

    private void ResolveParts()
    {
        if (skyDisc == null) skyDisc = FindImage("SkyDisc");
        if (timeRing == null) timeRing = FindImage("TimeRing");
        if (energyRing == null) energyRing = FindImage("EnergyFill");
        if (sunMarker == null) sunMarker = FindRect("SunMarker");
        if (moonMarker == null) moonMarker = FindRect("MoonMarker");
        if (weekdayText == null) weekdayText = FindText("Weekday");
        if (timeText == null) timeText = FindText("Time");
        if (periodText == null) periodText = FindText("Period");
        if (energyText == null) energyText = FindText("EnergyValue");
    }

    private Image FindImage(string name)
    {
        Transform found = transform.Find(name);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private RectTransform FindRect(string name)
    {
        return transform.Find(name) as RectTransform;
    }

    private TMP_Text FindText(string name)
    {
        Transform found = transform.Find(name);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private void Subscribe()
    {
        if (subscribed) return;

        if (book != null) book.PlannerChanged += Refresh;
        if (coreLoop != null) coreLoop.StateChanged += Refresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (book != null) book.PlannerChanged -= Refresh;
        if (coreLoop != null) coreLoop.StateChanged -= Refresh;
        subscribed = false;
    }

    /// <summary>Ubah jam dari script lain, misalnya saat waktu permainan maju.</summary>
    public void SetTime(int newHour, int newMinute)
    {
        hour = Mathf.Clamp(newHour, 0, 23);
        minute = Mathf.Clamp(newMinute, 0, 59);
        Refresh();
    }

    public void Refresh()
    {
        Resolve();

        float dayProgress = Mathf.Repeat(hour + minute / 60f, 24f) / 24f;
        float angle = dayProgress * 360f;
        float clock = hour + minute / 60f;

        if (skyDisc != null) skyDisc.color = SkyColor(clock);

        if (timeRing != null)
        {
            timeRing.type = Image.Type.Filled;
            timeRing.fillMethod = Image.FillMethod.Radial360;
            timeRing.fillOrigin = 2;
            timeRing.fillClockwise = true;
            timeRing.fillAmount = dayProgress;
            timeRing.color = clock >= 6f && clock < 19f
                ? new Color(0.83f, 0.65f, 0.24f, 1f)
                : new Color(0.58f, 0.65f, 0.86f, 1f);
        }

        int maxEnergy = book != null ? Mathf.Max(1, book.MaxEnergy) : 6;
        int energy = book != null ? book.Energy : 0;
        float energyFill = Mathf.Clamp01((float)energy / maxEnergy);

        if (energyRing != null)
        {
            energyRing.type = Image.Type.Filled;
            energyRing.fillMethod = Image.FillMethod.Radial360;
            energyRing.fillOrigin = 2;
            energyRing.fillClockwise = true;
            energyRing.fillAmount = energyFill;
            energyRing.color = energyFill < 0.25f ? lowEnergyColor : goodEnergyColor;
        }

        PlaceMarker(sunMarker, angle, markerRadius);
        PlaceMarker(moonMarker, angle, markerRadius);

        bool night = clock < 6f || clock >= 19f;
        if (sunMarker != null) sunMarker.gameObject.SetActive(!night);
        if (moonMarker != null) moonMarker.gameObject.SetActive(night);

        string weekday = coreLoop != null ? coreLoop.DayName : "Monday";
        if (weekdayText != null) weekdayText.text = weekday.ToUpperInvariant();
        if (timeText != null) timeText.text = hour.ToString("00") + ":" + minute.ToString("00");
        if (periodText != null) periodText.text = hour < 12 ? "AM" : "PM";
        if (energyText != null) energyText.text = energy.ToString();
    }

    private static void PlaceMarker(RectTransform marker, float angle, float radius)
    {
        if (marker == null) return;

        float radians = angle * Mathf.Deg2Rad;
        marker.anchoredPosition = new Vector2(
            Mathf.Sin(radians) * radius,
            Mathf.Cos(radians) * radius);
    }

    private Color SkyColor(float value)
    {
        if (value < 5f) return nightSky;
        if (value < 6.5f) return Color.Lerp(nightSky, dawnSky, Mathf.InverseLerp(5f, 6.5f, value));
        if (value < 8f) return Color.Lerp(dawnSky, daySky, Mathf.InverseLerp(6.5f, 8f, value));
        if (value < 16f) return daySky;
        if (value < 18.5f) return Color.Lerp(daySky, duskSky, Mathf.InverseLerp(16f, 18.5f, value));
        if (value < 20f) return Color.Lerp(duskSky, nightSky, Mathf.InverseLerp(18.5f, 20f, value));
        return nightSky;
    }
}
