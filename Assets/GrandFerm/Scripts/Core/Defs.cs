using System.Collections.Generic;
using UnityEngine;

namespace GrandFerm
{
    public enum Lang { UZ = 0, EN = 1, RU = 2 }

    public class CropDef
    {
        public string id, model, texture;
        public int seedCost, sellPrice, xp, unlockLevel, yield;
        public float growSeconds;
    }

    public class BuildingDef
    {
        public string id, model, texture, product, animalModel, animalTexture;
        public int cost, unlockLevel, feedCost, feedCycles, productPrice, xp, animals;
        public float cycleSeconds, animalScale;
        public Vector3 pos;
    }

    public static class Defs
    {
        public static readonly CropDef[] Crops =
        {
            new CropDef { id = "wheat",     model = "wheat",     texture = "wheat",            seedCost = 5,  sellPrice = 6,  xp = 4,  unlockLevel = 1, yield = 2, growSeconds = 20 },
            new CropDef { id = "corn",      model = "corn",      texture = "corn_leaf",        seedCost = 10, sellPrice = 14, xp = 8,  unlockLevel = 2, yield = 2, growSeconds = 35 },
            new CropDef { id = "sunflower", model = "sunflower", texture = "sunflower_petals", seedCost = 16, sellPrice = 22, xp = 12, unlockLevel = 3, yield = 2, growSeconds = 50 },
            new CropDef { id = "tomato",    model = "tomato",    texture = "tomato_skin",      seedCost = 24, sellPrice = 32, xp = 18, unlockLevel = 5, yield = 2, growSeconds = 70 },
            new CropDef { id = "cotton",    model = "cotton",    texture = "cotton",           seedCost = 35, sellPrice = 48, xp = 25, unlockLevel = 7, yield = 2, growSeconds = 95 },
        };

        public static readonly BuildingDef[] Buildings =
        {
            new BuildingDef { id = "coop",      model = "chicken_coop", texture = "wood",      product = "egg",  cost = 150, unlockLevel = 2, feedCost = 8,  feedCycles = 5, productPrice = 6,  xp = 3, animals = 2, cycleSeconds = 25, animalModel = "chicken", animalTexture = "chicken_feather", animalScale = 1f, pos = new Vector3(-10f, 0f, -5.5f) },
            new BuildingDef { id = "sheepfold", model = "sheep_shed",   texture = "barn_wall", product = "wool", cost = 400, unlockLevel = 3, feedCost = 15, feedCycles = 5, productPrice = 14, xp = 5, animals = 2, cycleSeconds = 45, animalModel = "sheep",   animalTexture = "sheep_wool",      animalScale = 1f, pos = new Vector3(-10f, 0f, 0f) },
            new BuildingDef { id = "cowshed",   model = "cow_shed",     texture = "red_paint", product = "milk", cost = 500, unlockLevel = 4, feedCost = 20, feedCycles = 5, productPrice = 18, xp = 6, animals = 2, cycleSeconds = 50, animalModel = "cow",     animalTexture = "cow_hide",        animalScale = 1f, pos = new Vector3(-10f, 0f, 5.5f) },
        };

        public static CropDef Crop(string id)
        {
            foreach (var c in Crops) if (c.id == id) return c;
            return null;
        }

        /// <summary>Sell price per unit for any inventory item (crop or animal product).</summary>
        public static int Price(string id)
        {
            var c = Crop(id);
            if (c != null) return c.sellPrice;
            foreach (var b in Buildings) if (b.product == id) return b.productPrice;
            return 0;
        }

        public static string[] AllItems()
        {
            var l = new List<string>();
            foreach (var c in Crops) l.Add(c.id);
            foreach (var b in Buildings) l.Add(b.product);
            return l.ToArray();
        }

        public static string IconName(string item)
        {
            return Crop(item) != null ? "crop_" + item : "product_" + item;
        }

        public static int XpForNext(int level) { return 40 + 30 * (level - 1); }
    }

    /// <summary>Tiny localisation: Uzbek / English / Russian.</summary>
    public static class L
    {
        public static Lang Current = Lang.UZ;

        static readonly Dictionary<string, string[]> T_ = new Dictionary<string, string[]>
        {
            // key           UZ                                   EN                                RU
            {"title",       new[]{"GRAND FERM","GRAND FERM","GRAND FERM"}},
            {"level",       new[]{"Daraja {0}","Level {0}","Уровень {0}"}},
            {"shop",        new[]{"Do'kon","Shop","Магазин"}},
            {"market",      new[]{"Bozor","Market","Рынок"}},
            {"sell",        new[]{"Sotish","Sell","Продать"}},
            {"sellAll",     new[]{"Hammasini sotish","Sell all","Продать всё"}},
            {"close",       new[]{"Yopish","Close","Закрыть"}},
            {"buy",         new[]{"Sotib olish","Buy","Купить"}},
            {"owned",       new[]{"Sotib olingan","Owned","Куплено"}},
            {"locked",      new[]{"Daraja {0}","Level {0}","Ур. {0}"}},
            {"noMoney",     new[]{"Pul yetarli emas","Not enough money","Недостаточно денег"}},
            {"lvlNeeded",   new[]{"Daraja {0} kerak","Level {0} required","Нужен уровень {0}"}},
            {"plowed",      new[]{"Yer shudgor qilindi","Field plowed","Поле вспахано"}},
            {"planted",     new[]{"Ekildi — endi sug'oring","Planted — now water it","Посажено — полейте"}},
            {"watered",     new[]{"Sug'orildi","Watered","Полито"}},
            {"thirsty",     new[]{"Ekin chanqadi — yana sug'oring","Crop is thirsty — water again","Растению нужна вода"}},
            {"growing",     new[]{"O'sib bormoqda: {0}%","Growing: {0}%","Растёт: {0}%"}},
            {"harvested",   new[]{"Hosil: +{0} {1}","Harvested: +{0} {1}","Урожай: +{0} {1}"}},
            {"fed",         new[]{"Hayvonlar oziqlantirildi","Animals fed","Животные накормлены"}},
            {"collected",   new[]{"Yig'ildi: +{0} {1}","Collected: +{0} {1}","Собрано: +{0} {1}"}},
            {"producing",   new[]{"Mahsulot tayyorlanmoqda…","Producing…","Производят…"}},
            {"sold",        new[]{"Sotildi: +{0}","Sold: +{0}","Продано: +{0}"}},
            {"nothing",     new[]{"Sotadigan narsa yo'q","Nothing to sell","Нечего продавать"}},
            {"levelUp",     new[]{"Yangi daraja: {0}!","Level up: {0}!","Новый уровень: {0}!"}},
            {"welcome",     new[]{"Xush kelibsiz! Yerni bosing.","Welcome! Tap the field.","Добро пожаловать! Коснитесь поля."}},
            {"bought",      new[]{"Sotib olindi!","Purchased!","Куплено!"}},
            {"feedInfo",    new[]{"Oziq: {0} so'm / {1} marta","Feed: {0} for {1} cycles","Корм: {0} на {1} раз"}},
            {"seeds",       new[]{"Urug'lar","Seeds","Семена"}},
            {"newGame",     new[]{"Yangi o'yin","New game","Новая игра"}},
            {"each",        new[]{"{0} so'm / dona","{0} each","{0} за шт."}},
            // items
            {"wheat",       new[]{"Bug'doy","Wheat","Пшеница"}},
            {"corn",        new[]{"Makkajo'xori","Corn","Кукуруза"}},
            {"sunflower",   new[]{"Kungaboqar","Sunflower","Подсолнух"}},
            {"tomato",      new[]{"Pomidor","Tomato","Помидор"}},
            {"cotton",      new[]{"Paxta","Cotton","Хлопок"}},
            {"egg",         new[]{"Tuxum","Egg","Яйцо"}},
            {"milk",        new[]{"Sut","Milk","Молоко"}},
            {"wool",        new[]{"Jun","Wool","Шерсть"}},
            // buildings
            {"coop",        new[]{"Tovuqxona","Chicken coop","Курятник"}},
            {"sheepfold",   new[]{"Qo'yxona","Sheepfold","Овчарня"}},
            {"cowshed",     new[]{"Sigirxona","Cowshed","Коровник"}},
        };

        public static string T(string key)
        {
            string[] v;
            return T_.TryGetValue(key, out v) ? v[(int)Current] : key;
        }

        public static string T(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        public static string Code { get { return Current == Lang.UZ ? "UZ" : Current == Lang.EN ? "EN" : "RU"; } }
        public static void Next() { Current = (Lang)(((int)Current + 1) % 3); }
    }
}
