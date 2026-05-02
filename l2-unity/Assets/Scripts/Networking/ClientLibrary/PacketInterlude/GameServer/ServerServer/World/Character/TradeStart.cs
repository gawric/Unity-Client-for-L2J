using System.Collections.Generic;

public class TradeStart : ServerPacket
{
    /// <summary>
    /// Player identifier
    /// </summary>
    private readonly int _player;
    private readonly List<ItemInstance> _itemList;

    public int PlayerId { get => _player; }
    public List<ItemInstance> ItemList { get => _itemList; }

    public TradeStart(byte[] d) : base(d)
    {
        Parse();
    }

    //public TradeStart(Player player)
    //{
    //    _player = player;
    //    _itemList = _player.GetInventory().GetAvailableItems(true, _player.IsGM && Config.GM_TRADE_RESTRICTED_ITEMS, false);
    //}

    public override void Parse()
    {
        int partnerObjectId = ReadI();
        int itemCount = ReadSh();

        // Проверяем, есть ли активный обмен
        if (partnerObjectId == 0 || itemCount == 0)
        {
            return; // Нет партнера или нет предметов для обмена
        }

        // Извлекаем предметы из буфера
        List<ItemInstance> itemList = new List<ItemInstance>();
        for (int i = 0; i < itemCount; i++)
        {
            int type1 = ReadSh(); // Тип предмета 1
            int objectId = ReadI(); // Object ID
            int itemId = ReadI(); // Item ID
            int count = ReadI(); // Количество
            int type2 = ReadSh(); // Тип предмета 2
            int unknownShort = ReadSh(); // Неизвестное значение (в оригинале 0)
            int bodyPart = ReadI(); // Слот (например, голова, руки и т.д.)
            int enchantLevel = ReadSh(); // Уровень зачарования
            int unknownShort2 = ReadSh(); // Неизвестное значение (в оригинале 0)
            int customType2 = ReadSh(); // Пользовательский тип 2

            // Воссоздаем объект Item
            //Item item = new Item(itemId, objectId, count, type1, type2, bodyPart, enchantLevel, customType2);
            //itemList.Add(item);
        }
    }
}