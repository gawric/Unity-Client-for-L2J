#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Verificación del perfil Lucera 166 con capturas reales del server (tests/capturas/**).
/// Para cada captura s2c: arma el paquete (opcode + body), lo pasa por el mismo parser que usa
/// Lucera166Protocol.TryParseGame y exige que el lector consuma TODOS los bytes.
/// No modifica código de producción: llama a los Parse* privados por reflexión.
///
/// Batch:  Unity -batchmode -quit -projectPath ... -executeMethod LuceraCaptureCheck.RunBatch
/// Salida: <repo>/docs/informes/LUCERA-CAPTURAS.md
/// </summary>
public static class LuceraCaptureCheck
{
    private static readonly Type P = typeof(Lucera166Protocol);

    // opcode -> (descripción, función que parsea con un lector propio y devuelve los bytes sobrantes)
    private static Dictionary<int, KeyValuePair<string, Func<byte[], int>>> BuildMap()
    {
        var m = new Dictionary<int, KeyValuePair<string, Func<byte[], int>>>();
        void Reader(int op, string method) =>
            m[op] = new KeyValuePair<string, Func<byte[], int>>(method, raw =>
            {
                var r = new PacketReader(raw);
                MethodInfo mi = P.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
                if (mi == null) throw new MissingMethodException(method);
                mi.Invoke(null, new object[] { r });
                return r.Remaining;
            });
        void Wire(int op, string typeName) =>
            m[op] = new KeyValuePair<string, Func<byte[], int>>("Wire<" + typeName + ">", raw =>
            {
                Type t = FindType(typeName);
                var dto = (IWireDto)Activator.CreateInstance(t);
                var r = new PacketReader(raw);
                dto.ReadFrom(r);
                return r.Remaining;
            });
        void WireThen(int op, string typeName, int trailing) =>
            m[op] = new KeyValuePair<string, Func<byte[], int>>("WireThen<" + typeName + "," + trailing + ">", raw =>
            {
                MethodInfo g = P.GetMethod("ParseWireThen", BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(FindType(typeName));
                var r = new PacketReader(raw);
                g.Invoke(null, new object[] { r, trailing });
                return r.Remaining;
            });
        void ReadInts(int op, string what, int n) =>
            m[op] = new KeyValuePair<string, Func<byte[], int>>(what, raw =>
            {
                var r = new PacketReader(raw);
                for (int i = 0; i < n; i++) r.ReadI();
                return r.Remaining;
            });
        void ReadInt(int op, string what) =>
            m[op] = new KeyValuePair<string, Func<byte[], int>>(what, raw =>
            {
                var r = new PacketReader(raw);
                r.ReadI();
                return r.Remaining;
            });

        Reader(0x2E, "ParseKeyPacket");
        Reader(0xB2, "ParsePetInfo");
        Reader(0x09, "ParseCharacterSelection");
        Reader(0x0B, "ParseCharSelected");
        Reader(0x0C, "ParseNpcInfo");
        Wire(0x2F, "CharMoveToLocationDto");
        Wire(0x47, "StopMoveDto");
        Wire(0x79, "ValidateLocationDto");
        Reader(0x11, "ParseItemList");
        Reader(0x21, "ParseInventoryUpdate");
        Reader(0x32, "ParseUserInfo");
        Reader(0x18, "ParseStatusUpdate");
        ReadInt(0x1F, "ActionFailed(ReadI)");
        WireThen(0x24, "TargetUnselectedDto", 4);
        Wire(0x22, "TeleportToLocationDto");
        Wire(0x25, "AutoAttackStartDto");
        Wire(0x26, "AutoAttackStopDto");
        ReadInt(0x01, "Revive(ReadI)");
        Wire(0x49, "MagicSkillCanceledDto");
        Reader(0x48, "ParseMagicSkillUse");
        Reader(0x54, "ParseMagicSkillLaunched");
        Reader(0x4A, "ParseSay2");
        Reader(0x00, "ParseDie");
        Reader(0x05, "ParseSpawnItem");
        Reader(0x16, "ParseDropItem");
        Reader(0x17, "ParseGetItem");
        Reader(0x33, "ParseAttack");
        Reader(0x31, "ParseCharInfo");
        Wire(0xC8, "PackageToListDto");
        ReadInts(0xB7, "PetDelete(2xReadI)", 2);
        ReadInt(0xA6, "TutorialShowHtml(ReadI)");
        Wire(0x7C, "ChooseInventoryItemDto");
        Wire(0x2A, "ManagePledgePowerDto");
        Reader(0xF5, "ParseShopPreviewList");
        Reader(0x41, "ParseWarehouseDeposit");
        Reader(0x42, "ParseWarehouseWithdraw");
        Reader(0xE9, "ParseBuyListSeed");
        Reader(0xD0, "ParseMultiSellList");
        Reader(0xD2, "ParsePackageSendable");
        Wire(0xDC, "RecipeBookItemListDto");
        Wire(0xDD, "RecipeItemMakeInfoDto");
        Reader(0x23, "ParseTargetSelected");
        WireThen(0x87, "EnchantResultDto", 28);
        Wire(0x27, "SocialActionDto");
        Wire(0x29, "ChangeWaitTypeDto");
        Reader(0x44, "ParseShortCutRegister");
        Reader(0x45, "ParseShortCutInit");
        Reader(0x5F, "ParseSkillList");
        Reader(0x62, "ParseSystemMessage");
        ReadInts(0x6B, "SetupGauge(4xReadI)", 4);
        WireThen(0x72, "MoveToPawnDto", 12);
        Reader(0x85, "ParseAbnormalStatus");
        WireThen(0x86, "QuestListDto", 128);
        Reader(0x89, "ParsePledgeInfo");
        Reader(0x90, "ParseAcquireSkillList");
        Reader(0x91, "ParseAcquireSkillInfo");
        Reader(0xB9, "ParseMyTargetSelected");
        Wire(0xC7, "SkillCoolTimeDto");
        Reader(0xF9, "ParseEtcStatusUpdate");
        Reader(0xFA, "ParseShortBuff");
        Wire(0x19, "NpcHtmlMessageDto");
        Reader(0x30, "ParseNpcSay");
        Wire(0x7B, "ShowCBoardDto");
        Wire(0x39, "AskJoinPartyDto");
        Reader(0x14, "ParseTradeStart");
        Reader(0x5B, "ParsePledgeMemberUpdate");
        Reader(0x5A, "ParsePledgeAll");
        Reader(0x5C, "ParsePledgeMemberAdd");
        Wire(0x5D, "PledgeShowMemberListDeleteDto");
        Wire(0xCD, "PledgeStatusChangedDto");
        ReadInts(0x08, "DeleteObject(2xReadI)", 2);
        Reader(0x0D, "ParseNewCharacterSuccess");
        ReadInt(0x0F, "CharacterCreateSuccess(ReadI)");
        ReadInt(0x10, "CharacterCreateFail(ReadI)");
        return m;
    }

    private static Type FindType(string name)
    {
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = a.GetTypes().FirstOrDefault(x => x.Name == name);
            if (t != null) return t;
        }

        throw new TypeLoadException(name);
    }

    private static string RepoRoot() =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));

    [MenuItem("EndlessWar/Lucera: verificar capturas reales")]
    public static void RunMenu() => Debug.Log(Run());

    public static void RunBatch()
    {
        string summary = Run();
        Debug.Log(summary);
        EditorApplication.Exit(summary.Contains("FALLAN: 0") ? 0 : 1);
    }

    private static string Run()
    {
        string root = RepoRoot();
        string capDir = Path.Combine(root, "tests", "capturas");
        var map = BuildMap();
        // En el juego NpcgrpTable se carga al iniciar; en batch queda vacía y NpcInfo daría NRE.
        FieldInfo grps = typeof(NpcgrpTable).GetField("_npcgrps", BindingFlags.NonPublic | BindingFlags.Instance);
        if (grps.GetValue(NpcgrpTable.Instance) == null)
            grps.SetValue(NpcgrpTable.Instance, new Dictionary<int, Npcgrp>());
        var stats = new SortedDictionary<string, int[]>(); // nombre -> [ok, fallos, sinHandler]
        var fails = new List<string>();
        int ok = 0, bad = 0, noHandler = 0, ex = 0, tryFalse = 0;

        foreach (string file in Directory.GetFiles(capDir, "*.bin", SearchOption.AllDirectories).OrderBy(f => f))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string[] parts = name.Split('_');
            if (parts.Length < 3) continue;
            int op = Convert.ToInt32(parts[1], 16);
            bool isEx = parts.Length >= 4 && parts[2].Length == 4 && IsHex(parts[2]);
            string pkt = isEx ? "FE:" + parts[2] + " " + string.Join("_", parts.Skip(3)) : string.Join("_", parts.Skip(2));
            byte[] body = File.ReadAllBytes(file);
            byte[] raw;
            if (isEx)
            {
                int sub = Convert.ToInt32(parts[2], 16);
                raw = new byte[3 + body.Length];
                raw[0] = 0xFE; raw[1] = (byte)sub; raw[2] = (byte)(sub >> 8);
                Buffer.BlockCopy(body, 0, raw, 3, body.Length);
            }
            else
            {
                raw = new byte[1 + body.Length];
                raw[0] = (byte)op;
                Buffer.BlockCopy(body, 0, raw, 1, body.Length);
            }

            if (!stats.TryGetValue(pkt, out int[] s)) stats[pkt] = s = new int[3];

            if (isEx || !map.TryGetValue(op, out var entry))
            {
                noHandler++; s[2]++;
                continue;
            }

            // 1) El camino real: TryParseGame sin cifrado no tiene que lanzar.
            try
            {
                var proto = (Lucera166Protocol)System.Runtime.Serialization.FormatterServices
                    .GetUninitializedObject(typeof(Lucera166Protocol));
                if (!proto.TryParseGame((byte[])raw.Clone(), false, out INetworkModel _)) tryFalse++;
            }
            catch (Exception e)
            {
                ex++; bad++; s[1]++;
                if (fails.Count < 400) fails.Add(Rel(root, file) + " | " + entry.Key + " | TryParseGame lanzó " + Inner(e));
                continue;
            }

            // 2) Consumo exacto.
            try
            {
                int left = entry.Value((byte[])raw.Clone());
                if (left == 0) { ok++; s[0]++; }
                else
                {
                    bad++; s[1]++;
                    if (fails.Count < 400) fails.Add(Rel(root, file) + " | " + entry.Key + " | sobran " + left + " bytes de " + body.Length);
                }
            }
            catch (Exception e)
            {
                bad++; s[1]++;
                if (fails.Count < 400) fails.Add(Rel(root, file) + " | " + entry.Key + " | lanzó " + Inner(e));
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Lucera 166 — capturas reales contra los parsers del cliente Unity");
        sb.AppendLine();
        sb.AppendLine("Generado por `Assets/Editor/LuceraCaptureCheck.cs` (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ").");
        sb.AppendLine();
        sb.AppendLine("OK: " + ok + "  FALLAN: " + bad + "  (excepciones en TryParseGame: " + ex +
                      ", TryParseGame=false: " + tryFalse + ")  sin handler Lucera: " + noHandler);
        sb.AppendLine();
        sb.AppendLine("| Paquete | OK | Fallan | Sin handler |");
        sb.AppendLine("|---|---:|---:|---:|");
        foreach (var kv in stats) sb.AppendLine("| " + kv.Key + " | " + kv.Value[0] + " | " + kv.Value[1] + " | " + kv.Value[2] + " |");
        sb.AppendLine();
        sb.AppendLine("## Fallos (primeros 400)");
        foreach (string f in fails) sb.AppendLine("- " + f);
        string outPath = Path.Combine(root, "docs", "informes", "LUCERA-CAPTURAS.md");
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
        return "LuceraCaptureCheck OK: " + ok + " FALLAN: " + bad + " sinHandler: " + noHandler + " -> " + outPath;
    }

    private static bool IsHex(string s) => s.All(c => Uri.IsHexDigit(c));
    private static string Inner(Exception e) => (e is TargetInvocationException t && t.InnerException != null ? t.InnerException : e).GetType().Name + ": " + (e.InnerException ?? e).Message;
    private static string Rel(string root, string f) => f.Substring(root.Length + 1).Replace('\\', '/');
}
#endif
