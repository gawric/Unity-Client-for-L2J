using System.Collections.Generic;

public class TradeStartDto : IWireDto
{
    /// <summary>
    /// Player identifier
    /// </summary>
    private int _player;
    private List<ItemInstance> _itemList = new List<ItemInstance>();

    public int PlayerId { get => _player; }
    public List<ItemInstance> ItemList { get => _itemList; }

    

    //public TradeStartDto(Player player)
    //{
    //    _player = player;
    //    _itemList = _player.GetInventory().GetAvailableItems(true, _player.IsGM && Config.GM_TRADE_RESTRICTED_ITEMS, false);
    //}

    public void ReadFrom(PacketReader reader)
    {
        _player = reader.ReadI();
        int itemCount = reader.ReadSh();

        // Проверяем, есть ли активный обмен
        if (_player == 0 || itemCount == 0)
        {
            return; // Нет партнера или нет предметов для обмена
        }

        // Извлекаем предметы из буфера
        List<ItemInstance> itemList = new List<ItemInstance>();
        for (int i = 0; i < itemCount; i++)
        {
            int type1 = reader.ReadSh(); // Тип предмета 1
            int objectId = reader.ReadI(); // Object ID
            int itemId = reader.ReadI(); // Item ID
            int count = reader.ReadI(); // Количество
            int type2 = reader.ReadSh(); // Тип предмета 2
            int unknownShort = reader.ReadSh(); // Неизвестное значение (в оригинале 0)
            int bodyPart = reader.ReadI(); // Слот (например, голова, руки и т.д.)
            int enchantLevel = reader.ReadSh(); // Уровень зачарования
            int unknownShort2 = reader.ReadSh(); // Неизвестное значение (в оригинале 0)
            int customType2 = reader.ReadSh(); // Пользовательский тип 2

            // Воссоздаем объект Item
            //Item item = new Item(itemId, objectId, count, type1, type2, bodyPart, enchantLevel, customType2);
            //itemList.Add(item);
        }
    }
}