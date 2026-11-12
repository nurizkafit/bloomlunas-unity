# Bloom Lunas — Buku Planner

Sistem ini melanjutkan `CoreLoop.cs`. Planner menyimpan rencana **7 hari**,
menampilkannya di CityOverWorld, lalu kembali ke planner setelah minggu selesai.
Setelah empat minggu, `CoreLoop` membuka **Act** untuk membayar utang.

Scene `MulaiGame` sekarang sudah memiliki UI planner, delapan halaman, tab, dan
day dial. Lihat [`Docs/MulaiGamePlannerUI.md`](MulaiGamePlannerUI.md) untuk isi
scene, kontrol, dan urutan tes. Langkah manual di bawah tetap berlaku kalau kamu
membuat scene planner baru atau memindahkan UI ke scene lain.

Script planner sendiri tidak membuat Canvas atau panel. Satu-satunya pembuatan
objek terjadi saat kartu dibuat dari `Card_Prefab`, atau saat planner membuka
rencana yang sudah tersimpan dan `Build Saved Cards` aktif.

## File yang dipakai

| File | Fungsi |
|---|---|
| `PlannerTypes.cs` | Tipe data kartu goal, rencana minggu, produk, market, ledger, skill, notes, quest. |
| `PlannerCatalog.cs` | Data awal kartu Explore/Craft/Open shop/Market + guild/Free time, produk, harga, jadwal, dan checklist. |
| `PlannerBook.cs` | State planner yang persisten, terhubung ke `CoreLoop`. |
| `DropSlot.cs` | Kotak hari; menerima kartu yang dijatuhkan. |
| `DraggableCard.cs` | Kartu goal yang bisa ditarik dari dek ke hari. |
| `PlannerBookUI.cs` | Controller halaman, header, review plan, dan drag/drop. |
| `PlannerPageView.cs` | Menampilkan isi Store, Ledger, Market, Hours, Skills, Notes, Quest. |
| `PlannerCityHUD.cs` | Menampilkan goal hari ini dan papan minggu di CityOverWorld. |
| `PlannerDeckDropZone.cs` | Mengembalikan kartu ke dek dan menghapus goal dari hari. |

`DropSlot.cs.meta` dan `DraggableCard.cs.meta` **wajib ikut disalin**. GUID di
dalamnya sama dengan GUID yang sudah tersimpan di `MulaiGame.unity` dan
`Card_Prefab.prefab`. Kalau `.meta` diganti, komponen di scene akan menjadi
`Missing Script`.

## Alur yang sudah terpasang

```text
Main Menu
  -> New Game (CoreLoop.StartNewGame)
  -> Planner minggu 1
       -> drag kartu ke Mon..Sun
       -> Review Plan / Finish Plan
       -> CoreLoop.BeginWeek
  -> CityOverWorld (7 hari)
       -> PlannerCityHUD membaca rencana hari ini
       -> Complete Goal (opsional) memberi 5 XP
       -> CoreLoop.CompleteDay
  -> DayReview -> ContinueAfterReview
  -> Minggu selesai -> WeekReview
       -> ContinueAfterWeek
            minggu 1-3: Planner minggu berikutnya (rencana baru)
            minggu 4: Act -> bayar utang -> Retro
  -> StartNextMonth -> Planner bulan berikutnya
```

Rencana minggu berjalan disimpan di `PlannerBook` sehingga tetap ada saat
pindah dari `MulaiGame` ke `CityOverWorld`. Saat minggu berganti, rencana lama
disimpan sebagai `PreviousWeek`, lalu tujuh hari baru dibuat kosong.

## 1. Pasang state planner

1. Buka `MainMenu`.
2. Pilih GameObject `GameSystems` yang sudah berisi `CoreLoop`.
3. Add Component -> `PlannerBook`.
4. Biarkan `Core Loop` kosong; script akan mengambil `CoreLoop` dari objek yang sama.
5. `PlannerBook` memakai `DontDestroyOnLoad`, jadi cukup satu untuk seluruh game.

Kalau kamu membuka `MulaiGame` langsung dari Editor, `CoreLoop` dan `PlannerBook`
tidak ada. Mulailah dari `MainMenu`, atau tambahkan `GameSystems` yang sama ke
`MulaiGame` untuk pengujian.

## 2. Pasang controller di scene MulaiGame

1. Buka `Assets/Scenes/MulaiGame.unity`.
2. Pilih GameObject `Canvas/PlannerBookUI`.
3. Add Component -> `PlannerBookUI`.
4. Di bagian **Halaman Buku** (warisan `PlannerBookManager`), isi:
   - `Goals Page` -> `PagesHost/GoalsPage`
   - `Store Page` -> `PagesHost/StorePage`
   - `Ledger Page` -> `PagesHost/LedgerPage`
   - `Market Page` -> `PagesHost/MarketPage`
   - `Skills Page` -> `PagesHost/SkillsPage`
   - `Notes Page` -> `PagesHost/NotesPage`
   - `Hours Page` -> halaman baru (lihat langkah 5)
   - `Quest Page` -> halaman baru (lihat langkah 5)
5. Isi **Header** dengan TMP_Text yang sudah ada:
   - `Week Label` -> `Header/WeekLabel`
   - `Gold Label` -> `Header/Stat_GOLD/Value`
   - `Debt Label` -> `Header/Stat_DEBT/Value`
   - `Next Payment Label` -> `Header/Stat_NEXT PAYMENT/Value`
   - `Energy Label` -> `Header/Stat_Energy/Value`
   - `Gold After Label` -> `Header/Stat_GOLD AFTER PLAN/Value`
   - `Warning Chip` -> `Header/WarningChip`
   - `Warning Text` -> `Header/WarningChip/Text`
6. Isi **Goals**:
   - `Slots` boleh kosong; controller akan mengambil semua `DropSlot` di bawahnya.
   - `Card Prefab` -> `Assets/Card_Prefab.prefab`
   - `Deck Container` -> `GoalsPage/BookCover/Pages/RightPage/CardsView/Viewport/CardsContainer`
   - `Goal Count Label` -> `GoalsPage/BookCover/Pages/LeftPage/GoalCount`
   - `Build Saved Cards` biarkan aktif agar rencana yang tersimpan tampil sebagai kartu.
7. Isi **Review Plan**:
   - `Review Modal` -> `ReviewModal`
   - `Review Summary Label` -> `ReviewModal/Panel/SummaryViewport/Summary`
   - `Review Warning Label` -> `ReviewModal/Panel/WarningBox/Text`
   - `Start Anyway Button` -> `ReviewModal/Panel/StartAnyway`
   - `Keep Planning Button` -> `ReviewModal/Panel/KeepPlanning`
   - `Close Review Button` -> `ReviewModal/Panel/CloseX`
   - `Review Plan Button` -> `Header/ReviewButton` (atau tombol Finish plan buatanmu)

## 3. EventSystem yang belum ada

Scene `MulaiGame` belum memiliki `EventSystem`, sehingga tombol dan drag tidak
akan menerima input.

1. Di `MulaiGame`, klik kanan Hierarchy -> **UI -> Event System**.
2. Pastikan `Canvas` memiliki `Graphic Raycaster` (sudah ada di scene ini).
3. Jangan menambahkan EventSystem kedua.

## 4. Siapkan dek kartu

`Card_Prefab` sudah memiliki `DraggableCard`. Kamu tidak perlu mengubah script
prefab.

1. Buka `Card_Prefab.prefab` di Prefab Mode.
2. Duplikat prefab ke `CardsContainer` lima kali (atau lebih).
3. Pada setiap instance, di komponen `DraggableCard`:
   - centang `Is Deck Template`
   - pilih `Preset Goal`: `Explore`, `Craft`, `OpenShop`, `MarketGuild`, `FreeTime`
   - biarkan `Apply Preset On Awake` aktif
4. Kartu template tetap berada di dek. Saat dijatuhkan ke hari, planner membuat
   salinan kartu untuk hari itu.
5. Kartu hasil salinan memiliki `Is Deck Template` mati, sehingga bisa dipindah
   antarhari atau dihapus lewat tombol `Delete`.

Kalau kamu ingin kartu sekali pakai, matikan `Is Deck Template`; kartu akan
berpindah dari dek ke hari, bukan menyalin.

## 5. Tambahkan halaman Hours dan Quest

Scene lama baru memiliki Goals, Store, Ledger, Market, Skills, dan Notes.

1. Di `PagesHost`, duplikat `NotesPage`.
2. Rename menjadi `HoursPage`.
3. Duplikat lagi menjadi `QuestPage`.
4. Pada masing-masing page, add component `PlannerPageView`.
5. Set `Page Id`:
   - `HoursPage` -> `Hours`
   - `QuestPage` -> `Quest`
6. Isi `Title Text`, `Subtitle Text`, `Body Text`, dan `Footer Text` dengan TMP
   yang ada di page tersebut. `Right Text` opsional.
7. Masukkan kedua page baru ke field `Hours Page` dan `Quest Page` di
   `PlannerBookUI`.

## 6. Pasang view untuk enam halaman lama

Untuk setiap page di bawah `PagesHost`, add component `PlannerPageView` dan isi
`Page Id`:

| Page | Page Id | TMP yang biasanya dipakai |
|---|---|---|
| StorePage | Store | `Page/Title`, `Page/Subtitle`, `Page/Body`, `Page/Footer` |
| LedgerPage | Ledger | sama |
| MarketPage | Market | sama |
| SkillsPage | Skills | sama |
| NotesPage | Notes | sama |
| GoalsPage | Goals | cukup `Title`, `Subtitle`, `Footer`; jangan isi `Body` agar baris hari tidak tertimpa |

`Right Text` bisa diarahkan ke TMP kolom kanan kalau page-mu punya kolom kanan.
Kalau tidak ada, biarkan kosong; isi utama tetap tampil di `Body Text`.

## 7. Tab Rail

`TabRail` sudah memiliki tombol Goals, Store, Ledger, Market, Skills, Notes.
Belum ada tab Hours dan Quest.

1. Duplikat salah satu tab, rename menjadi `Tab_Hours` dan `Tab_Quest`.
2. Di `PlannerBookUI`, bagian **Tab Rail**, isi tombol:
   `Tab Goals`, `Tab Store`, `Tab Ledger`, `Tab Market`, `Tab Hours`,
   `Tab Skills`, `Tab Notes`, `Tab Quest`.
3. Biarkan `Auto Wire Tabs` aktif. Controller akan memasang `SwitchPage` sendiri.

Kalau kamu lebih suka manual, matikan `Auto Wire Tabs` dan hubungkan OnClick ke
`PlannerBookUI.SwitchPage(string)` dengan argumen `Store`, `Ledger`, `Market`,
`Hours`, `Skill`, `Notes`, atau `Quest`.

## 8. Tombol plan dan minggu

| Tombol | Method |
|---|---|
| Review / Finish plan | `PlannerBookUI.ReviewPlan` |
| Start anyway | `PlannerBookUI.StartWeekAnyway` |
| Keep planning | `PlannerBookUI.CloseReviewModal` |
| Finish plan (hanya jika 7 hari terisi) | `PlannerBookUI.FinishPlan` |
| End day | `CoreLoop.CompleteDay` |
| Continue check | `CoreLoop.ContinueAfterReview` |
| Next month (Retro) | `CoreLoop.StartNextMonth` |

`ReviewPlan` hanya membuka ringkasan. `FinishPlan` memulai minggu kalau semua
tujuh hari sudah punya goal; kalau belum, ringkasan tetap dibuka.

## 9. CityOverWorld

1. Buka `CityOverWorld`.
2. Di Canvas kota, buat panel teks manual (atau pakai panel yang sudah ada).
3. Add component `PlannerCityHUD` ke panel itu.
4. Isi:
   - `Book` kosong (otomatis mengambil `PlannerBook.Instance`)
   - `Core Loop` kosong
   - `Week Label`, `Day Label`, `Goal Label`, `Status Label`, `Debt Label`
   - `Day Goal Labels` dengan 7 TMP kalau kamu ingin papan minggu
   - `Complete Goal Button` dengan tombol yang memanggil `PlannerCityHUD.CompleteGoal`
5. Tombol `Complete Goal` memberi 5 XP ke skill yang sesuai dan menandai goal
   hari itu selesai. Setelah itu tekan `End Day` seperti biasa.

`PlannerCityHUD` tidak membuat panel. Kalau tidak ingin memakai HUD, cukup baca
`PlannerBook.Instance.GetDayGoal(CoreLoop.Instance.DayIndex)` dari script UI-mu.

## 10. Interaksi halaman

`PlannerPageView` menyediakan method untuk tombol/stepper:

- `AddProductUnit(string productId)` dan `RemoveProductUnit(string productId)`
- `AddStoreJar`, `RemoveStoreJar`, `AddPersonalJar`, `RemovePersonalJar`,
  `AddBufferJar`, `RemoveBufferJar`
- `SetMarketBand(string "itemId:band")`, contoh `kunyit_asam:2`
  (0 Low, 1 Fair, 2 High)
- `ToggleFirstStep(int index)`
- `DeliverCommission(string commissionId)`

Semua method langsung mengubah `PlannerBook` dan me-refresh teks.

## 11. Urutan tes

1. Buka `MainMenu`, tekan Play.
2. New Game -> Planner minggu 1.
3. Pastikan `GameSystems` hanya satu dan `PlannerBook` ada.
4. Drag satu kartu ke hari Senin.
5. Isi tujuh hari; `Goal Count` menjadi `7/7`.
6. Review Plan -> Finish Plan.
7. Di CityOverWorld, HUD menampilkan goal hari ini.
8. Complete Goal -> End Day -> Continue sampai Minggu.
9. Setelah WeekReview, Continue -> Planner minggu 2 dengan tujuh hari kosong.
10. Ulangi sampai minggu 4 -> Act -> bayar utang -> Retro.
11. Dari Retro, Next Month -> Planner bulan berikutnya.

## Catatan penting

- Jangan menaruh `PlannerBook` di setiap scene. Cukup satu di `GameSystems`.
- Jangan menaruh `PlannerBookUI` di CityOverWorld; UI planner hanya di `MulaiGame`.
- `DropSlot` dan `DraggableCard` memakai GUID lama; jangan hapus `.meta` yang disediakan.
- Script tidak memanggil `Instantiate` untuk membuat Canvas/panel. Kartu hanya
  dibuat dari prefab saat rencana disimpan atau saat kartu dek dijatuhkan.
- Nilai biaya hidup, utilitas, dan pembayaran minimum tetap mengikuti Inspector
  `CoreLoop`, bukan angka hardcode di Planner.
- Untuk interaksi gedung, label, zoom, konfirmasi, dan karakter berjalan di
  `CityOverWorld`, lihat [`Docs/CityOverWorld.md`](CityOverWorld.md).
