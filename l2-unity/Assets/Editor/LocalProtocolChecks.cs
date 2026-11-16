using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class LocalProtocolChecks
{
    public static void Run()
    {
        CheckChat();
        LocalPartyChecks.Run();
        LocalDuelChecks.Run();
        LocalMerchantChecks.Run();
        LocalGroundChecks.Run();
        foreach (string command in new[] { "admin", "announce OK", "announce Привет семье" })
        {
            var packet = new SendBypassBuildCmd(new SendBypassBuildCmdCommand(command));
            byte[] wire = packet.GetData();
            byte[] expected = Encoding.Unicode.GetBytes(command + "\0");
            Require(wire[0] == 0x5B, "GM chat must use builder opcode 0x5B");
            Require(wire.Length >= expected.Length + 1, "Truncated builder command");
            for (int i = 0; i < expected.Length; i++)
                Require(wire[i + 1] == expected[i], "Builder command must be null-terminated UTF-16LE without admin_ prefix");
        }
        Debug.Log("[LocalProtocolChecks] GM builder: 3 wire cases passed");
    }

    public static void CheckChat()
    {
        foreach (int type in new[] { 0, 2, 3, 10 })
        {
            foreach (string text in new[] { "OK", "A", "Привет семье", "" })
            {
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)0x4A);
                    writer.Write(123);
                    writer.Write(type);
                    writer.Write(Encoding.Unicode.GetBytes("Sender\0"));
                    writer.Write(Encoding.Unicode.GetBytes(text + "\0"));
                    var packet = new CreatureSayDto();
                    packet.ReadFrom(new PacketReader(stream.ToArray()));
                    string name = type == 10 ? "Announcements" : "Sender";
                    string expected = name + ": " + text + "</color>";
                    Require(packet.Message != null && packet.Message.ToString().EndsWith(expected, StringComparison.Ordinal),
                        "Interlude chat must preserve the full text: type=" + type + " expected=" + text);
                }
            }
        }
        Debug.Log("[LocalProtocolChecks] Interlude chat: 16 cases passed");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
