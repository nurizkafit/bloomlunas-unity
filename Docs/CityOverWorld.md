# Bloom Lunas — CityOverWorld

Dokumen ini melanjutkan [`PlannerBook.md`](PlannerBook.md). Fokusnya hanya
interaksi kota:

```text
klik gedung / label
  -> kamera zoom ke gedung
  -> panel konfirmasi (Go / Cancel)
  -> Go: kamera kembali, karakter berjalan ke titik lokasi
  -> animasi karakter berjalan
```

Script **tidak membuat Canvas, label, panel, tombol, collider, atau karakter**
saat Play. Semua objek dan referensi dipasang manual di Unity Editor.

## File

| File | Fungsi |
|---|---|
| `Assets/Script/CityLocation.cs` | Data satu lokasi: nama, deskripsi, titik fokus kamera, offset zoom, tujuan jalan. |
| `Assets/Script/CityLocationLabel.cs` | Label UI yang mengikuti posisi gedung dan bisa diklik. |
| `Assets/Script/CityOverWorldController.cs` | Raycast gedung, zoom kamera, panel konfirmasi, dan perintah jalan. |
| `Assets/Script/CharacterMovement.cs` | Sudah ada; memindahkan karakter ke tujuan. |
| `Assets/Script/CharacterProceduralAnimator.cs` | Sudah ada; animasi idle/jalan dari node `leg-left`, `leg-right`, `arm-left`, `arm-right`, `torso`, `head`. |
| `Assets/Script/IsometricCameraFollow.cs` | Sudah ada; kamera yang dinonaktifkan sementara saat zoom. |

Tidak ada scene atau prefab yang diubah oleh script ini.

## 1. Petakan empat lokasi

Scene `CityOverWorld` sudah memiliki model dan `LocationPoint`. Gunakan
pemetaan berikut agar sesuai permintaan:

| Objek di `Envi` | Nama tampilan | `LocationPoint` lama |
|---|---|---|
| `unit-house` | Home and Shop | Home |
| `unit-mill` | Market | Market |
| `building-archery` | Guild Hall | Adventurer Guild |
| `grass-forest` | City Outskirt | Gathering area |
| `building-farm` | Home Drying Yard (opsional) | Home drying yard |

`CityLocation` memakai `LocationPoint` yang sudah ada untuk mengambil
`WalkPoint`. Jadi kamu tidak perlu memindahkan titik jalan yang lama.

## 2. Pasang CityLocation pada gedung

Lakukan untuk empat objek yang dipakai:

1. Pilih root prefab instance, misalnya `Envi/unit-house`.
2. **Add Component -> City Location**.
3. Isi:
   - `Display Name` -> `Home and Shop`, `Market`, `Guild Hall`, atau `City Outskirt`.
   - `Description` -> misalnya `Open Mon-Sat 06:00-17:00.`
   - `Location Point` boleh kosong; akan dicari otomatis di root.
4. `Focus Point` boleh kosong. Script memakai posisi gedung + `Focus Height`.
5. Atur `Camera Offset` jika hasil zoom terlalu jauh/dekat. Nilai awal
   `(2.3, 5.8, -5.8)` cocok untuk kamera isometrik scene ini.
6. `Focus Field Of View` awal `34`. Isi `0` kalau tidak ingin FOV berubah.
7. `Walk Point` boleh kosong karena `LocationPoint` sudah punya titik jalan.

Jika kamu ingin membuat titik fokus khusus:

1. Buat empty child di gedung, misalnya `FocusPoint`.
2. Letakkan di tengah gedung, sekitar tinggi 1.5–2 m.
3. Masukkan ke field `Focus Point` pada `CityLocation`.

## 3. Tambahkan collider agar gedung bisa diklik

Model FBX Kenney di scene ini **tidak punya collider**. Tanpa collider, klik
badan gedung tidak akan terdeteksi (label UI tetap bisa diklik).

Untuk setiap root gedung:

1. **Add Component -> Box Collider**.
2. Pastikan `Is Trigger` **mati**.
3. Klik `Edit Collider`, lalu tarik kotak agar menutup badan gedung.
4. Jika collider terasa terlalu besar karena root berskala 3.5, coba:
   - `Center`: `(0, 0.4, 0)`
   - `Size`: `(1.1, 0.8, 1.1)`
5. Ulangi untuk `unit-mill`, `building-archery`, dan `grass-forest`
   (untuk area outskirt, collider boleh lebih lebar).

`CityOverWorldController` memakai `Physics.RaycastAll`, lalu mencari
`CityLocation` pada collider atau parent-nya. Jadi collider boleh berada di
root gedung atau di child mesh.

## 4. Buat label di atas gedung

Label memakai UI biasa, bukan world-space TextMesh. Script mengubah posisi
label mengikuti proyeksi gedung ke layar.

1. Pilih `Canvas`.
2. Buat child UI: **UI -> Text - TextMeshPro**.
3. Rename menjadi `Label_Home`, `Label_Market`, `Label_Guild`, `Label_Outskirt`.
4. Atur RectTransform:
   - Anchor/pivot: center
   - Size: sekitar `260 x 44`
   - Posisi awal bebas; script akan menggesernya.
5. Buat tampilan label (opsional tapi disarankan):
   - Tambahkan `Image` sebagai background rounded.
   - Tambahkan `Button` dengan `Target Graphic` = Image tersebut.
   - Pastikan `Raycast Target` pada Image/TMP **aktif**.
6. Tambahkan `CanvasGroup` pada label. Ini dipakai untuk menyembunyikan label
   saat kamera zoom/konfirmasi.
7. **Add Component -> City Location Label**.
8. Isi:
   - `Location` -> `CityLocation` pada gedung yang sesuai.
   - `Label Root` -> RectTransform label (boleh kosong jika komponen ada di label itu).
   - `Label Text` -> TMP label.
   - `Button` -> Button label (opsional; jika kosong, klik TMP langsung).
   - `Canvas Group` -> CanvasGroup label.
   - `World Camera` -> `Main Camera`.
   - `World Offset` -> `(0, 3.2, 0)`; sesuaikan sampai label berada di atas atap.
9. Nama teks akan diisi otomatis dari `CityLocation.DisplayName`.

Jika label tidak memakai Button, TMP harus memiliki `Raycast Target` aktif dan
Canvas harus memiliki `Graphic Raycaster`. Scene ini sudah memiliki
`EventSystem` dan `Graphic Raycaster`.

## 5. Buat panel konfirmasi

1. Di `Canvas`, buat panel UI, misalnya `CityConfirmPanel`.
2. Tambahkan background Image dan dua TMP:
   - `Title` -> nama lokasi
   - `Description` -> deskripsi/jam buka
3. Tambahkan dua Button:
   - `Go` (teks `Go`)
   - `Cancel` (teks `Cancel` atau `Back`)
4. Biarkan panel **tidak aktif** di scene; controller akan mengaktifkannya
   saat gedung selesai di-zoom.
5. Buat empty GameObject `CityInteraction` (boleh di root scene atau di Canvas).
6. **Add Component -> City OverWorld Controller**.
7. Isi referensi:
   - `World Camera` -> `Main Camera`
   - `Character` -> `character-e` (karakter pemain)
   - `Camera Follow` -> komponen `IsometricCameraFollow` di `Main Camera`
   - `Confirm Panel` -> `CityConfirmPanel`
   - `Title Text` -> TMP Title
   - `Description Text` -> TMP Description
   - `Go Button` -> tombol Go
   - `Cancel Button` -> tombol Cancel
8. `Zoom Duration` awal `0.45`. `Drag Threshold Pixels` awal `7`; klik yang
   bergeser lebih dari itu dianggap orbit kamera, bukan klik gedung.

Tombol Go dan Cancel **di-wire otomatis** oleh controller saat `Awake`. Kalau
kamu juga memasang listener manual, panggil method yang sama:
`ConfirmGo` dan `Cancel`.

## 6. Pasang animasi karakter

Model `character-e` sudah memiliki node tubuh:

```text
root
  leg-left
  leg-right
  torso
  arm-left
  arm-right
  head
```

1. Pilih `character-e`.
2. Pastikan sudah ada `CharacterMovement`.
3. **Add Component -> Character Procedural Animator**.
4. Tidak perlu Animator Controller atau file animasi.
5. Saat `CharacterMovement.IsMoving` bernilai true, script memutar kaki dan
   tangan, menganggukkan torso/kepala, dan menambahkan napas saat idle.
6. Jika karakter menghadap ke belakang saat berjalan, putar model/visual
   `character-e` 180 derajat; jangan mengubah arah gerak `CharacterMovement`.

`CharacterProceduralAnimator` juga dipakai warga yang dibuat `NpcCrowd`, jadi
animasi yang sama berlaku untuk NPC.

## 7. Urutan tes

1. Buka `CityOverWorld`.
2. Tekan Play.
3. Klik label `Home and Shop` atau badan `unit-house`.
4. Kamera harus zoom ke gedung dan panel konfirmasi muncul.
5. Klik `Cancel` atau tekan `Escape`; kamera kembali tanpa karakter berjalan.
6. Klik lagi, lalu `Go`.
7. Kamera kembali ke tampilan kota; karakter berjalan ke `WalkPoint` gedung.
8. Ulangi untuk Market, Guild Hall, dan City Outskirt.
9. Saat karakter berjalan, label disembunyikan; setelah tiba, label muncul lagi.

## Catatan

- Klik gedung membutuhkan collider. Klik label tidak membutuhkan collider gedung.
- `CityOverWorldController` menonaktifkan `IsometricCameraFollow` sementara saat
  zoom, lalu mengembalikannya. Kamera tidak dihapus/diganti.
- `CityLocation` memakai `LocationPoint` lama untuk tujuan jalan; kamu tetap
  bisa mengisi `Walk Point` baru jika ingin titik lain.
- `CityOverWorldController` hanya memindahkan karakter. Perpindahan ke scene
  `House`, `Market`, dan sebagainya belum dijalankan di tahap ini.
- Untuk mengubah jarak zoom per gedung, atur `Camera Offset` di
  `CityLocation` masing-masing; tidak perlu mengubah script.
