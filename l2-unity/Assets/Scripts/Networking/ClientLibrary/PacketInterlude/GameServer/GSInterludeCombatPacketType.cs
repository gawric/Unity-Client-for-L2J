using UnityEngine;

public enum GSInterludeCombatPacketType : byte
{
    MyTargetSelected = 0xA6,
    MoveToPawn = 0x60,
    STOP_MOVE = 0x47,
    ATTACK = 0x05,
    AUTO_ATTACK_START = 0x2B,
    AUTO_ATTACK_STOP = 0x2C,
    DIE = 0x06,
    CharMoveToLocation = 0x01,
    MagicSkillUse = 0x48,
    SetupGauge = 0x6D,
    ItemList = 0x1B,
    ChooseInventoryItem = 0x6f,
    EnchantResult = 0x81,
    InventoryUpdate = 0x27,
    MagicSkillLaunched = 0x76,
    ActionFailed = 0x25,
}
