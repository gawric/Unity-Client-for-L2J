using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class LocalDuelChecks
{
    public static void Run()
    {
        var outgoing = new OutgoingPacketAutoRegistry();
        foreach (bool party in new[] { false, true })
        {
            byte[] wire = outgoing.Create(new RequestDuelStartCommand("Mage", party)).GetData();
            Header(wire, 0x27);
            byte[] name = Encoding.Unicode.GetBytes("Mage\0");
            for (int i = 0; i < name.Length; i++)
                Check(wire[i + 3] == name[i], "Duel target encoding");
            Check(BitConverter.ToInt32(wire, 3 + name.Length) == (party ? 1 : 0), "Duel kind field");
            foreach (bool accepted in new[] { false, true })
            {
                wire = outgoing.Create(new RequestDuelAnswerStartCommand(party, accepted)).GetData();
                Header(wire, 0x28);
                Check(BitConverter.ToInt32(wire, 3) == (party ? 1 : 0) && BitConverter.ToInt32(wire, 7) == 0 &&
                    BitConverter.ToInt32(wire, 11) == (accepted ? 1 : 0), "Answer must contain party, reserved, response");
            }
        }
        Header(outgoing.Create(new RequestDuelSurrenderCommand()).GetData(), 0x30);
        byte[] attack = outgoing.Create(new AttackRequestCommand(202, -71200, 258100, -3043)).GetData();
        Check(attack[0] == 0x0A && BitConverter.ToInt32(attack, 1) == 202 && BitConverter.ToInt32(attack, 5) == -71200 &&
            BitConverter.ToInt32(attack, 9) == 258100 && BitConverter.ToInt32(attack, 13) == -3043 && attack[17] == 0,
            "Forced attack opcode, target, origin and shift byte");

        var incoming = new IncomingPacketAutoRegistry();
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)0xFE);
            writer.Write((short)0x4B);
            writer.Write(Encoding.Unicode.GetBytes("Challenger\0"));
            writer.Write(0);
            Check(incoming.TryParseGame(new ItemServer(stream.ToArray()), out var parsed) && parsed is ExDuelAskStartDto invitation &&
                invitation.Name == "Challenger" && !invitation.Party, "FE invitation dispatch must skip both extended opcode bytes");
        }
        foreach (byte id in new byte[] { 0x4C, 0x4D, 0x4E })
        {
            byte[] wire = { 0xFE, id, 0, 1, 0, 0, 0 };
            Check(incoming.TryParseGame(new ItemServer(wire), out var parsed) && parsed is DuelPhaseDto phase && phase.Party,
                "Extended duel phase registry");
        }
        DuelFighterInfo fighter;
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)0xFE);
            writer.Write((short)0x4F);
            writer.Write(Encoding.Unicode.GetBytes("Mage\0"));
            foreach (int value in new[] { 202, 10, 1, 80, 98, 56, 102, 21, 49 })
                writer.Write(value);
            Check(incoming.TryParseGame(new ItemServer(stream.ToArray()), out var parsed) && parsed is ExDuelUpdateUserInfoDto,
                "Duel stats registry");
            fighter = ((ExDuelUpdateUserInfoDto)parsed).Fighter;
            Check(fighter.ObjectId == 202 && fighter.ClassId == 10 && fighter.Hp == 80 && fighter.MaxHp == 98 &&
                fighter.Mp == 56 && fighter.MaxMp == 102 && fighter.Cp == 21 && fighter.MaxCp == 49, "Duel stat layout");
        }
        var state = new PlayerDuel();
        state.Invite("Mage", false);
        Check(state.Phase == DuelPhase.Invitation, "Invitation state");
        state.AwaitStart();
        state.Ready(false);
        state.Start(false);
        state.Update(fighter);
        Check(state.Phase == DuelPhase.Active && state.Fighters.Count == 1, "Active duel and damage state");
        state.Clear();
        state.Update(fighter);
        Check(state.Phase == DuelPhase.None && state.Fighters.Count == 0 && state.OpponentName == null,
            "Late stats after duel end must not recreate duel UI");
        state.Challenge("Mage", false);
        state.Ready(false);
        Check(state.OpponentName == "Mage", "Ready preserves challenged target");
        Debug.Log("[LocalDuelChecks] Extended duel layouts, transitions, registry and forced attack passed");
    }

    static void Header(byte[] wire, byte sub)
    {
        Check(wire.Length >= 3 && wire[0] == 0xD0 && wire[1] == sub && wire[2] == 0, "Extended client packet header");
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
