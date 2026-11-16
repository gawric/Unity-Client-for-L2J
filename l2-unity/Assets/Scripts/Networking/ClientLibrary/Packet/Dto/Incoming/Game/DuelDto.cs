using System.IO;

public sealed class ExDuelAskStartDto : IWireDto
{
    public string Name;
    public bool Party;

    public void ReadFrom(PacketReader reader)
    {
        Name = reader.ReadOtherS();
        if (string.IsNullOrEmpty(Name) || !reader.HasRemaining(4))
            throw new InvalidDataException("Truncated duel invitation");
        Party = reader.ReadI() != 0;
    }
}

public abstract class DuelPhaseDto : IWireDto
{
    public bool Party;
    public void ReadFrom(PacketReader reader)
    {
        if (!reader.HasRemaining(4))
            throw new InvalidDataException("Truncated duel phase");
        Party = reader.ReadI() != 0;
    }
}

public sealed class ExDuelReadyDto : DuelPhaseDto { }
public sealed class ExDuelStartDto : DuelPhaseDto { }
public sealed class ExDuelEndDto : DuelPhaseDto { }

public sealed class ExDuelUpdateUserInfoDto : IWireDto
{
    public DuelFighterInfo Fighter;
    public void ReadFrom(PacketReader reader)
    {
        var info = new DuelFighterInfo { Name = reader.ReadOtherS() };
        if (string.IsNullOrEmpty(info.Name) || !reader.HasRemaining(36))
            throw new InvalidDataException("Truncated duel fighter");
        info.ObjectId = reader.ReadI();
        info.ClassId = reader.ReadI();
        info.Level = reader.ReadI();
        info.Hp = reader.ReadI();
        info.MaxHp = reader.ReadI();
        info.Mp = reader.ReadI();
        info.MaxMp = reader.ReadI();
        info.Cp = reader.ReadI();
        info.MaxCp = reader.ReadI();
        Fighter = info;
    }
}
