using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class CharSelectInfoPackage
{
    private PlayerInterludeAppearance _appearance;
    private PaperDollSelection _paperDool;
    private PlayerInterludeStats _stats;

    public CharSelectInfoPackage()
    {
        _appearance = new PlayerInterludeAppearance();
        _paperDool = new PaperDollSelection();
        _stats = new PlayerInterludeStats();
    }

    private int _slot;
    private string _name;
    private int _characterId;
    private string _accountName;
    private int _accountId;
    private int _clan;
    private int _buildLvl;
    private CharacterRaceAnimation _characterRaceAnimation;
    private int _baseClassId;
    private int _gsName;
    private int _lvl;
    private int _delTime;
    private bool _selected;

    private double _hp;
    private double _mp;
    private byte _enchantEffect;
    private int _activeId;
    private int _augmentationId;


    public PlayerInterludeAppearance Appreance { get => _appearance; set => _appearance = value; }
    public int Slot { get => _slot; set => _slot = value; }
    public string Name { get => _name; set => _name = value; }
    public int ObjId { get => _characterId; set => _characterId = value; }
    public string Account { get => _accountName; set => _accountName = value; }
    public int AccountId { get => _accountId; set => _accountId = value; }
    public int ClanId { get => _clan; set => _clan = value; }
    public int BuildLvl { get => _buildLvl; set => _buildLvl = value; }
    public int Sex { get => _appearance.Sex; set => _appearance.Sex = value; }
    public int Race { get => _appearance.Race; set => _appearance.Race = value; }
    public int ClassId { get => _baseClassId; set => _baseClassId = value; }
    public byte EnchantId { get => _enchantEffect; set => _enchantEffect = value; }
    public int ActiveId { get => _activeId; set => _activeId = value; }
    public int GsName { get => _gsName; set => _gsName = value; }
    public int DlTimer { get => _delTime; set => _delTime = value; }

    public int Autgmentation { get => _augmentationId; set => _augmentationId = value; }

    public double Hp { get => _hp; set => _hp = value; }
    public double Mp { get => _mp; set => _mp = value; }
    public double Sp { get => _stats.Sp; set => _stats.Sp = value; }
    public double Exp { get => _stats.Exp; set => _stats.Exp = value; }
    public int Lvl { get => _lvl; set => _lvl = value; }
    public int Karma { get => _stats.Karma; set => _stats.Karma = value; }
    public int HairStyle { get => _appearance.HairStyle; set => _appearance.HairStyle = value; }
    public int HairColor { get => _appearance.HairColor; set => _appearance.HairColor = value; }
    public int Face { get => _appearance.Face; set => _appearance.Face = value; }

    public double MaxHp { get => _stats.MaxHp; set => _stats.MaxHp = value; }
    public double MaxMp { get => _stats.MaxMp; set => _stats.MaxMp = value; }
    // LevelServer.GetExp(level);
    public double MaxExp() { return LevelServer.GetExp(_lvl + 1); }


    public bool Selected { get => _selected; set => _selected = value; }
    public double ExpProcent { get => _stats.ExpPercent(_lvl + 1); }
    public PaperDollSelection PaperDoll { get => _paperDool; }

    public CharacterRaceAnimation CharacterRaceAnimation { get => _characterRaceAnimation; set => _characterRaceAnimation = value; }


}
