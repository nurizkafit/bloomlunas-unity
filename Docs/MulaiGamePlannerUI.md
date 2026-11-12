# MulaiGame — Buku Planner dan Day Dial

Dokumen ini menjelaskan isi `Assets/Scenes/MulaiGame.unity` setelah UI planner
dipasang. Semua sprite yang dipakai adalah aset yang sudah ada di
`Assets/image/UI`; tidak ada gambar baru yang dibuat.

## Yang sudah terpasang di scene

| Bagian | Isi |
|---|---|
| `Canvas/PlannerBookUI` | Komponen `PlannerBookUI`, controller buku. |
| `BookArea/PagesHost` | 8 halaman: Goals, Store, Ledger, Market, Hours, Skills, Notes, Quest. |
| Setiap halaman | Komponen `PlannerPageView` dengan `Page Id` yang sesuai. |
| `BookArea/TabRail` | 8 tombol tab, otomatis terhubung oleh `PlannerBookUI`. |
| `DayDial` | Jam dengan komponen terpisah: sky disc, time ring, energy ring, sun/moon, dan teks waktu. |
| `BookArea` | Ornamen sudut `CornerTL/TR/BL/BR` dari sprite `ORN-01`. |

`PlannerBookUI` mencari halaman, tab, header, modal, dan kartu dek secara
otomatis saat `Awake`. Kalau kamu mengubah nama objek, isi field di Inspector
secara manual.

## Kontrol tiap halaman

| Halaman | Kontrol |
|---|---|
| Goals | Tarik kartu goal dari dek ke hari, atau klik dek lalu klik hari. Kartu bisa dipindah antarhari dan dihapus. |
| Store | `[-]` dan `[+]` di tiap produk mengubah unit. `[-]` dan `[+]` di tiap jar mengubah budget. Semua perubahan langsung tersimpan di `PlannerBook`. |
| Ledger | Menampilkan pemasukan, pengeluaran, jar terpakai, dan entri terbaru. |
| Market | `L`, `F`, `H` di tiap produk mengubah pita harga (Low, Fair, High). |
| Hours | Jadwal buka tiap lokasi, biaya mingguan, dan tagihan akhir bulan. |
| Skills | Level dan XP Moon, Sun, Earth. |
| Notes | `[ ]` dan `[x]` di First Steps bisa diklik; catatan minggu lalu ditampilkan. |
| Quest | Commission yang siap bisa di-`Deliver`; request yang masih terbuka ditampilkan. |

Semua tombol kecil itu memakai *TMP link*, bukan panel baru. `PlannerPageView`
menangani kliknya lewat `OnPointerClick`.

## Day dial

`DayDial` tersusun dari komponen UI terpisah:

- `SkyDisc` — warna langit berubah mengikuti jam (malam, fajar, siang, senja).
- `TimeRing` — `Image` dengan `Image Type = Filled`, `Fill Method = Radial 360`,
  `Fill Origin = Top`.
- `EnergyFill` — ring energi dari `PlannerBook.Energy`.
- `SunMarker` dan `MoonMarker` — ikon matahari/bulan bergerak mengelilingi dial.
- `Weekday`, `Time`, `Period`, `EnergyValue` — teks TMP.

Jam bisa diubah dari script lain:

```csharp
PlannerDayDialUI dial = FindAnyObjectByType<PlannerDayDialUI>();
dial.SetTime(18, 30);
```

Saat ini `hour` dan `minute` di Inspector dipakai sebagai sumber waktu. Kalau
nanti ada sistem waktu permainan, panggil `SetTime` dari sana.

## Tampilan terbaru

- Setiap halaman punya badge judul dari `ICN-01_medallion_default` dan ikon
  halaman dari `ICN-02_icon_*` (store, ledger, market, hours, skills, note, quest,
  calendar).
- Halaman satu-badan memakai panel dalam `FRM-07_inset-panel_default` di belakang
  teks supaya isi lebih terpisah dari halaman buku.
- Kartu goal dikecilkan menjadi 56 px, memakai `CRD-08_small-goal-card-skin_default`,
  dan menampilkan ikon gambar asli (`IconArt`) sesuai `goalType`.
- Jarak antar-kartu di dek dikurangi dari 10 menjadi 4.
- Semua sprite di atas diambil dari `Assets/Image/UI`; tidak ada art baru.
- `DropSlot` sekarang memakai `dayIndex` unik 0-6, jadi menaruh satu kartu tidak
  lagi mengubah semua hari.

## Catatan gameplay

- Data planner disimpan di `PlannerBook` dan memakai `DontDestroyOnLoad`, jadi
  rencana tetap ada saat pindah ke `CityOverWorld`.
- Kalau scene `MulaiGame` dibuka langsung tanpa lewat `MainMenu`,
  `PlannerBookUI` membuat `CoreLoop` dan `PlannerBook` di runtime lalu memanggil
  `StartNewGame`. Kalau kamu masuk dari `MainMenu`, sistem yang sudah ada dipakai
  tanpa duplikat.
- Kartu dek dibuat saat `Awake` dari `Assets/Card_Prefab.prefab` dan
  `PlannerCatalog`. Kartu tidak disimpan sebagai objek scene supaya mudah diubah.
- Jam, ring, dan ornamen tidak mengubah data gameplay. Mereka hanya tampilan.

## Urutan tes

1. Buka scene `MulaiGame` lalu Play. Kalau mulai dari `MainMenu`, tekan New Game.
2. Tab Goals: pastikan 5 kartu goal muncul di dek.
3. Tarik satu kartu ke hari Senin. `GoalCount` berubah menjadi `1/7`.
4. Tab Store: tekan `[+]` satu produk dan `[+]` jar Store; teks ikut berubah.
5. Tab Market: tekan `L`, `F`, atau `H`; harga Now berubah.
6. Tab Notes: klik `[ ]` First Steps; jumlah berubah.
7. Tab Quest: kalau ada commission siap, tekan `Deliver`.
8. Tab Ledger, Hours, dan Skills untuk memastikan data terbaca.
9. Kembali ke Goals dan tekan Review / Finish plan.
10. Masuk ke `CityOverWorld` dan pastikan rencana yang sama terbaca.
