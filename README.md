# GRAND FERM — 3D Offline Farming Simulator (Android)

Offline ferma simulyatori. Unity 2022.3 LTS (built-in render pipeline), mobil uchun.

## O'ynash
- **Yerni bosing:** shudgor → ekish (pastdagi urug'ni tanlang) → sug'orish → o'sish → yana sug'orish (50% da) → hosil.
- **Hayvon binolari** (Do'kon): bosing — oziq bering, mahsulot tayyor bo'lsa yig'ing (🟡 = tayyor, 🔴 = oziq kerak).
- **Bozor** (pastdagi tugma yoki bozor peshtaxtasi): mahsulotni soting, pul ishlang, daraja oshiring.
- Bir barmoq — kamerani siljitish, ikki barmoq — yaqinlashtirish. Til: UZ / EN / RU. O'yin avtomatik saqlanadi (offline o'sish 8 soatgacha davom etadi).

## Ishga tushirish
1. Unity Hub → Add → shu papka (2022.3.x). Birinchi ochilganda `GrandFerm → Repair project setup` avtomatik ishlaydi (material, sahna, Android sozlamalari).
2. `Assets/GrandFerm/Scenes/Main.unity` ni oching va Play bosing. O'yin sahnasi kod orqali quriladi (`Scripts/Core`).
3. Android: File → Build Settings → Android → Build.

## APK (GitHub Actions)
`.github/workflows/build-android.yml` — har bir push'da APK yig'adi (Actions → Artifacts). Kerakli secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` ([GameCI](https://game-ci.com/docs/github/activation)).

## Tuzilma
- `Assets/GrandFerm/Resources/{Models,Textures,Icons}` — grafika (OBJ modellar UV bilan, tekstura, ikonka)
- `Assets/GrandFerm/Scripts/Core` — `Defs` (ekin/bino jadvali, til), `GameState` (qoidalar, saqlash), `World` (3D sahna), `GameUI`
- `Art_Source/SVG` — asl SVG ikonkalar
- Balans: `Defs.cs` ichida.

## Ma'lum cheklovlar
- Past poligonli blokout modellar (starter pack). Hayvonlar yo'nalishi `AnimalWander.modelYaw` bilan to'g'rilanadi.
- Kod Unity'da hali kompilyatsiya qilib sinalmagan — birinchi ochilishda xatolar chiqsa, Console'dagi matnni yuboring.
