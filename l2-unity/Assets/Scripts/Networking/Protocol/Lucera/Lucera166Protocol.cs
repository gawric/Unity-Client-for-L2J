using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Rama Lucera Classic 166 (lobby). Login delegado a Interlude a la espera de validar
/// Init/LoginOk/PlayOk; gameserver Lucera implementado para ProtocolVersion, KeyPacket,
/// AuthLogin y CharacterSelectionInfo. El resto del lobby/mundo todavía no.
/// Ver docs/MIGRACION-LUCERA.md.
/// </summary>
public sealed class Lucera166Protocol : IProtocol
{
    private const byte KeyPacketOpcode = 0x2E;
    private const byte CharacterSelectionOpcode = 0x09;
    private const byte CharSelectedOpcode = 0x0B;
    private const byte NpcInfoOpcode = 0x0C;
    private const byte NewCharacterSuccessOpcode = 0x0D;
    private const byte DeleteObjectOpcode = 0x08;
    private const byte CharMoveToLocationOpcode = 0x2F;
    private const byte StopMoveOpcode = 0x47;
    private const byte ValidateLocationOpcode = 0x79;
    private const byte ItemListOpcode = 0x11;
    private const byte InventoryUpdateOpcode = 0x21;
    private const byte UserInfoOpcode = 0x32;
    private const byte StatusUpdateOpcode = 0x18;
    private const byte ActionFailOpcode = 0x1F;
    private const byte TargetUnselectedOpcode = 0x24;
    private const byte SocialActionOpcode = 0x27;
    private const byte ChangeWaitTypeOpcode = 0x29;
    private const byte ShortCutRegisterOpcode = 0x44;
    private const byte ShortCutInitOpcode = 0x45;
    private const byte SkillListOpcode = 0x5F;
    private const byte SystemMessageOpcode = 0x62;
    private const byte SetupGaugeOpcode = 0x6B;
    private const byte MoveToPawnOpcode = 0x72;
    private const byte NormalStatusUpdateOpcode = 0x85;    private const byte QuestListOpcode = 0x86;
    private const byte PledgeInfoOpcode = 0x89;
    private const byte AcquireSkillListOpcode = 0x90;
    private const byte AcquireSkillInfoOpcode = 0x91;
    private const byte MyTargetSelectedOpcode = 0xB9;
    private const byte SkillCoolTimeOpcode = 0xC7;
    private const byte HennaInfoOpcode = 0xE5;
    private const byte EtcStatusUpdateOpcode = 0xF9;
    private const byte ShortBuffStatusUpdateOpcode = 0xFA;
    private const byte NpcHtmlMessageOpcode = 0x19;
    private const byte NpcSayOpcode = 0x30;
    private const byte AskJoinPartyOpcode = 0x39;
    private const byte TradeDoneOpcode = 0x1C;
    private const byte TradeStartOpcode = 0x14;
    private const byte PledgeShowMemberAllOpcode = 0x5A;
    private const byte PledgeShowMemberUpdateOpcode = 0x5B;
    private const byte PledgeShowMemberAddOpcode = 0x5C;
    private const byte PledgeShowMemberDeleteOpcode = 0x5D;
    private const byte PledgeShowMemberDeleteAllOpcode = 0x88;
    private const byte ShowBoardOpcode = 0x7B;
    private const byte PetInfoOpcode = 0xB2;
    private const byte PledgeStatusChangedOpcode = 0xCD;
    private const byte TeleportToLocationOpcode = 0x22;
    private const byte AutoAttackStartOpcode = 0x25;
    private const byte AutoAttackStopOpcode = 0x26;
    private const byte ReviveOpcode = 0x01;
    private const byte MagicSkillCanceledOpcode = 0x49;
    private const byte MagicSkillUseOpcode = 0x48;
    private const byte MagicSkillLaunchedOpcode = 0x54;
    private const byte Say2Opcode = 0x4A;
    private const byte DieOpcode = 0x00;
    private const byte SpawnItemOpcode = 0x05;
    private const byte DropItemOpcode = 0x16;
    private const byte GetItemOpcode = 0x17;
    private const byte AttackOpcode = 0x33;
    private const byte CharInfoOpcode = 0x31;
    private const byte PackageToListOpcode = 0xC8;
    private const byte PetDeleteOpcode = 0xB7;
    private const byte TutorialShowHtmlOpcode = 0xA6;
    private const byte ChooseInventoryItemOpcode = 0x7C;
    private const byte ManagePledgePowerOpcode = 0x2A;
    private const byte ShopPreviewListOpcode = 0xF5;
    private const byte WarehouseDepositOpcode = 0x41;
    private const byte WarehouseWithdrawOpcode = 0x42;
    private const byte BuyListSeedOpcode = 0xE9;
    private const byte MultiSellListOpcode = 0xD0;
    private const byte PackageSendableListOpcode = 0xD2;
    private const byte RecipeBookItemListOpcode = 0xDC;
    private const byte RecipeItemMakeInfoOpcode = 0xDD;
    private const byte TargetSelectedOpcode = 0x23;
    private const byte EnchantResultOpcode = 0x87;
    private const byte CharacterCreateSuccessOpcode = 0x0F;
    private const byte CharacterCreateFailOpcode = 0x10;

    private readonly InterludeProtocol _loginProtocol;
    private readonly object _gameCryptLock = new object();
    private L2_login.GameCrypt _gameCrypt;

    public Lucera166Protocol(
        IIncomingPacketAutoRegistry packets,
        IOutgoingPacketAutoRegistry outgoing)
    {
        _loginProtocol = new InterludeProtocol(packets, outgoing);
    }

    public int ProtocolVersion
    {
        get { return 166; }
    }

    public IOutgoingPacket EncodeLogin(INetworkCommand command)
    {
        return _loginProtocol.EncodeLogin(command);
    }

    public bool TryParseLogin(byte[] raw, bool init, bool cryptEnabled, out INetworkModel model)
    {
        return _loginProtocol.TryParseLogin(raw, init, cryptEnabled, out model);
    }

    public void SetLoginBlowfishKey(byte[] key)
    {
        _loginProtocol.SetLoginBlowfishKey(key);
    }

    public IOutgoingPacket EncodeGame(INetworkCommand command, bool encrypt)
    {
        byte[] data;
        if (command is ProtocolVersionCommand protocol)
        {
            data = LuceraWire.ProtocolVersion(protocol.Protocol);
        }
        else if (command is AuthLoginCommand auth)
        {
            data = LuceraWire.AuthLogin(auth);
        }
        else if (command is CharacterSelectCommand select)
        {
            data = LuceraWire.CharacterSelected(select.Slot);
        }
        else if (command is CharacterDeleteCommand delete)
        {
            data = LuceraWire.CharacterDelete(delete.Slot);
        }
        else if (command is CharacterCreateCommand create)
        {
            PlayerTemplates template = ResolveTemplate(create);
            data = LuceraWire.CharacterCreate(
                create.Name,
                ResolveRace(create.RaceName),
                create.Sex == "Female" ? 1 : 0,
                template._classId,
                template.Base_str,
                template.Base_dex,
                template.Base_con,
                template.Base_int,
                template.Base_wit,
                template.Base_men,
                ParseAppearance(create.HairStyle),
                ParseAppearance(create.HairColor),
                ParseAppearance(create.Face));
        }
        else if (command is NewCharacterCommand)
        {
            data = LuceraWire.NoPayload(0x13);
        }
        else if (command is EnterWorldCommand)
        {
            data = LuceraWire.NoPayload(0x11);
        }
        else if (command is AppearingCommand)
        {
            data = LuceraWire.NoPayload(0x3A);
        }
        else if (command is RequestSay2Command say)
        {
            data = LuceraWire.Say2(say.Message, say.Data != null ? say.Data.Type : 0, say.TargetName);
        }
        else if (command is MoveToCommand move)
        {
            UnityEngine.Vector3 from = VectorUtils.ConvertPosUnityToL2j(move.From);
            UnityEngine.Vector3 to = VectorUtils.ConvertPosUnityToL2j(move.To);
            data = LuceraWire.MoveTo(
                (int)System.Math.Round(to.x), (int)System.Math.Round(to.y), (int)System.Math.Round(to.z),
                (int)System.Math.Round(from.x), (int)System.Math.Round(from.y), (int)System.Math.Round(from.z));
        }
        else if (command is UseItemCommand useItem)
        {
            data = LuceraWire.UseItem(useItem.ObjectId, useItem.CtrlPressed);
        }
        else if (command is ClickActionCommand click)
        {
            data = LuceraWire.Action(click.ObjectId, click.OriginX, click.OriginY, click.OriginZ, (byte)click.ActionId);
        }
        else if (command is RequestMagicSkillUseCommand skill)
        {
            data = LuceraWire.MagicSkillUse(skill.SkillId, skill.CtrlPressed, skill.ShiftPressed);
        }
        else if (command is RequestBypassToServerCommand bypass)
        {
            data = LuceraWire.Bypass(bypass.Bypass);
        }
        else if (command is RequestTargetCanceldCommand)
        {
            data = LuceraWire.TargetCancel(0);
        }
        else if (command is RequestRestartPointCommand)
        {
            data = LuceraWire.RestartPoint(0);
        }
        else if (command is RequestSkillCoolTimeCommand)
        {
            data = LuceraWire.SkillCoolTime();
        }
        else if (command is RequestShowBoardCommand)
        {
            data = LuceraWire.ShowBoard(0);
        }
        else if (command is AnswerTradeRequestCommand answerTrade)
        {
            data = LuceraWire.AnswerTrade(answerTrade.Response);
        }
        else if (command is AddTradeItemCommand addTrade)
        {
            data = LuceraWire.AddTradeItem(addTrade.Trade, addTrade.ObjectId, addTrade.Count);
        }
        else if (command is RequestAnswerJoinPartyCommand answerParty)
        {
            data = LuceraWire.AnswerJoinParty(answerParty.Response);
        }
        else if (command is MultiSellChooseCommand multiSell)
        {
            data = LuceraWire.MultiSellChoose(multiSell.ListId, multiSell.EntryId, multiSell.Amount);
        }
        else if (command is RequestDestroyItemCommand destroy)
        {
            data = LuceraWire.DestroyItem(destroy.ObjectId, destroy.Count);
        }
        else if (command is RequestEnchantItemCommand enchant)
        {
            data = LuceraWire.EnchantItem(enchant.ObjectId);
        }
        else if (command is ValidatePositionCommand validate)
        {
            UnityEngine.Vector3 pos = VectorUtils.ConvertPosUnityToL2j(
                new UnityEngine.Vector3(validate.X, validate.Y, validate.Z));
            data = LuceraWire.Ints(0x59,
                (int)System.Math.Round(pos.x), (int)System.Math.Round(pos.y),
                (int)System.Math.Round(pos.z), validate.Heading, 0); // + D vehículo (ValidatePosition.java)
        }
        else if (command is RequestShortCutRegCommand shortcutReg)
        {
            data = LuceraWire.Ints(0x3D, shortcutReg.TypeId, shortcutReg.WorldSlot, shortcutReg.Id, shortcutReg.Level, 0); // + D (RequestShortCutReg.java)
        }
        else if (command is RequestShortCutDelCommand shortcutDel)
        {
            data = LuceraWire.Ints(0x3F, shortcutDel.WorldSlot);
        }
        else if (command is RequestQuestAbortCommand questAbort)
        {
            data = LuceraWire.Ints(0x63, questAbort.QuestId);
        }
        else if (command is RequestPledgeInfoCommand pledgeInfo)
        {
            data = LuceraWire.Ints(0x65, pledgeInfo.ClanId);
        }
        else if (command is RequestJoinPledgeCommand joinPledge)
        {
            data = LuceraWire.Ints(0x26, joinPledge.ObjectId, 0); // + D pledgeType (RequestJoinPledge.java)
        }
        else if (command is RequestOustPledgeMemberCommand oust)
        {
            data = LuceraWire.StringInts(0x29, oust.MemberName);
        }
        else if (command is RequestGiveNickNameCommand nick)
        {
            data = LuceraWire.NickName(nick.MemberName, nick.Title);
        }
        else if (command is RequestPledgePowerCommand pledgePower)
        {
            data = pledgePower.Action == 2
                ? LuceraWire.Ints(0xCC, pledgePower.Rank, pledgePower.Action, pledgePower.Privs)
                : LuceraWire.Ints(0xCC, pledgePower.Rank, pledgePower.Action); // RequestPledgePower.java
        }
        else if (command is RequestAcquireSkillCommand acquireSkill)
        {
            data = LuceraWire.Ints(0x7C, acquireSkill.SkillId, acquireSkill.SkillLevel, acquireSkill.SkillType);
        }
        else if (command is RequestAcquireSkillInfoCommand acquireInfo)
        {
            data = LuceraWire.Ints(0x73, acquireInfo.SkillId, acquireInfo.SkillLevel, acquireInfo.SkillType);
        }
        else if (command is RequestDropItemCommand drop)
        {
            data = LuceraWire.DropItem(drop.ObjectId, drop.Count, drop.X, drop.Y, drop.Z);
        }
        else if (command is RequestRecipeBookOpenCommand recipeBook)
        {
            data = LuceraWire.Ints(0xB5, recipeBook.IsDwarven);
        }
        else if (command is RequestRecipeItemMakeInfoCommand recipeInfo)
        {
            data = LuceraWire.Ints(0xB7, recipeInfo.RecipeId);
        }
        else if (command is RequestRecipeItemMakeSelfCommand recipeSelf)
        {
            data = LuceraWire.Ints(0xB8, recipeSelf.RecipeId);
        }
        else
        {
            throw new NotSupportedException(
                "Lucera 166: comando de juego no implementado: " + command.GetType().Name);
        }

        if (encrypt && _gameCrypt != null)
        {
            lock (_gameCryptLock)
            {
                _gameCrypt.Encrypt(data, 0, data.Length);
            }
        }

        return new LuceraRawOutgoing(data);
    }

    public bool TryParseGame(byte[] raw, bool cryptEnabled, out INetworkModel model)
    {
        model = null;
        if (raw == null || raw.Length == 0)
        {
            return false;
        }

        if (cryptEnabled)
        {
            lock (_gameCryptLock)
            {
                if (_gameCrypt == null)
                {
                    return false;
                }

                _gameCrypt.Decrypt(raw, 0, raw.Length);
            }
        }

        PacketReader reader = new PacketReader(raw);
        bool parsed = TryParseGameCore(raw, reader, out model);
        if (parsed && reader.Remaining != 0)
        {
            // Diagnóstico: el layout leído no coincide con el del server (Lucera 166).
            UnityEngine.Debug.LogWarning("[Lucera] paquete 0x" + raw[0].ToString("X2") + " (" +
                (model != null ? model.GetType().Name : "?") + "): sobran " + reader.Remaining +
                " de " + (raw.Length - 1) + " bytes");
        }

        return parsed;
    }

    private static bool TryParseGameCore(byte[] raw, PacketReader reader, out INetworkModel model)
    {
        model = null;
        switch (raw[0])
        {
            case KeyPacketOpcode:
                model = ParseKeyPacket(reader);
                return model != null;

            case CharacterSelectionOpcode:
                model = ParseCharacterSelection(reader);
                return model != null;

            case CharSelectedOpcode:
                model = ParseCharSelected(reader);
                return model != null;

            case NpcInfoOpcode:
                model = ParseNpcInfo(reader);
                return model != null;

            case CharMoveToLocationOpcode:
                model = ParseWireThen<CharMoveToLocationDto>(reader, 0);
                return model != null;

            case StopMoveOpcode:
                model = ParseWireThen<StopMoveDto>(reader, 0);
                return model != null;

            case ValidateLocationOpcode:
                model = ParseWireThen<ValidateLocationDto>(reader, 0);
                return model != null;

            case ItemListOpcode:
                model = ParseItemList(reader);
                return model != null;

            case InventoryUpdateOpcode:
                model = ParseInventoryUpdate(reader);
                return model != null;

            case UserInfoOpcode:
                model = ParseUserInfo(reader);
                return model != null;

            case StatusUpdateOpcode:
                model = ParseStatusUpdate(reader);
                return model != null;

            case ActionFailOpcode:
                model = ActionFailedDto.From(reader.ReadI());
                return model != null;

            case TargetUnselectedOpcode:
                model = ParseWireThen<TargetUnselectedDto>(reader, 4); // + D 0 final de Lucera
                return model != null;

            case TeleportToLocationOpcode:
                model = ParseWireThen<TeleportToLocationDto>(reader, 0);
                return model != null;

            case AutoAttackStartOpcode:
                model = ParseWireThen<AutoAttackStartDto>(reader, 0);
                return model != null;

            case AutoAttackStopOpcode:
                model = ParseWireThen<AutoAttackStopDto>(reader, 0);
                return model != null;

            case ReviveOpcode:
                model = new ReviveDto { ObjectId = reader.ReadI() };
                return true;

            case MagicSkillCanceledOpcode:
                model = ParseWireThen<MagicSkillCanceledDto>(reader, 0);
                return model != null;

            case MagicSkillUseOpcode:
                model = ParseMagicSkillUse(reader);
                return model != null;

            case MagicSkillLaunchedOpcode:
                model = ParseMagicSkillLaunched(reader);
                return model != null;

            case Say2Opcode:
                model = ParseSay2(reader);
                return model != null;

            case DieOpcode:
                model = ParseDie(reader);
                return model != null;

            case SpawnItemOpcode:
                model = ParseSpawnItem(reader);
                return model != null;

            case DropItemOpcode:
                model = ParseDropItem(reader);
                return model != null;

            case GetItemOpcode:
                model = ParseGetItem(reader);
                return model != null;

            case AttackOpcode:
                model = ParseAttack(reader);
                return model != null;

            case CharInfoOpcode:
                model = ParseCharInfo(reader);
                return model != null;

            case PackageToListOpcode:
                model = ParseWireThen<PackageToListDto>(reader, 0);
                return model != null;

            case PetDeleteOpcode:
                reader.ReadI();
                model = new DeleteObjectDto { ObjectId = reader.ReadI() };
                return true;

            case TutorialShowHtmlOpcode:
                reader.ReadI();
                model = TutorialShowHtmlDto.From(reader.ReadOtherS());
                return true;

            case ChooseInventoryItemOpcode:
                model = ParseWireThen<ChooseInventoryItemDto>(reader, 0);
                return model != null;

            case ManagePledgePowerOpcode:
                model = ParseWireThen<ManagePledgePowerDto>(reader, 0);
                return model != null;

            case ShopPreviewListOpcode:
                model = ParseShopPreviewList(reader);
                return model != null;

            case WarehouseDepositOpcode:
                model = ParseWarehouseDeposit(reader);
                return model != null;

            case WarehouseWithdrawOpcode:
                model = ParseWarehouseWithdraw(reader);
                return model != null;

            case BuyListSeedOpcode:
                model = ParseBuyListSeed(reader);
                return model != null;

            case MultiSellListOpcode:
                model = ParseMultiSellList(reader);
                return model != null;

            case PackageSendableListOpcode:
                model = ParsePackageSendable(reader);
                return model != null;

            case RecipeBookItemListOpcode:
                model = ParseWireThen<RecipeBookItemListDto>(reader, 0);
                return model != null;

            case RecipeItemMakeInfoOpcode:
                model = ParseWireThen<RecipeItemMakeInfoDto>(reader, 0);
                return model != null;

            case TargetSelectedOpcode:
                model = ParseTargetSelected(reader);
                return model != null;

            case EnchantResultOpcode:
                model = ParseWireThen<EnchantResultDto>(reader, 28); // + D, Q, D×3, D (EnchantResult.java)
                return model != null;

            case SocialActionOpcode:
                model = ParseWireThen<SocialActionDto>(reader, 0);
                return model != null;

            case ChangeWaitTypeOpcode:
                model = ParseWireThen<ChangeWaitTypeDto>(reader, 0);
                return model != null;

            case ShortCutRegisterOpcode:
                model = ParseShortCutRegister(reader);
                return model != null;

            case ShortCutInitOpcode:
                model = ParseShortCutInit(reader);
                return model != null;

            case SkillListOpcode:
                model = ParseSkillList(reader);
                return model != null;

            case SystemMessageOpcode:
                model = ParseSystemMessage(reader);
                return model != null;

            case SetupGaugeOpcode:
                reader.ReadI(); // SetupGauge.java (Lucera): D objectId antes de color/tiempos
                model = ParseWireThen<SetupGaugeDto>(reader, 0);
                return model != null;

            case MoveToPawnOpcode:
                model = ParseWireThen<MoveToPawnDto>(reader, 12); // + destino x,y,z de Lucera
                return model != null;

            case NormalStatusUpdateOpcode:
                model = ParseAbnormalStatus(reader);
                return model != null;

            case QuestListOpcode:
                model = ParseWireThen<QuestListDto>(reader, 128); // + B(128) de Lucera
                return model != null;

            case PledgeInfoOpcode:
                model = ParsePledgeInfo(reader);
                return model != null;

            case AcquireSkillListOpcode:
                model = ParseAcquireSkillList(reader);
                return model != null;

            case AcquireSkillInfoOpcode:
                model = ParseAcquireSkillInfo(reader);
                return model != null;

            case MyTargetSelectedOpcode:
                model = ParseMyTargetSelected(reader);
                return model != null;

            case SkillCoolTimeOpcode:
                model = ParseWireThen<SkillCoolTimeDto>(reader, 0);
                return model != null;

            case EtcStatusUpdateOpcode:
                model = ParseEtcStatusUpdate(reader);
                return model != null;

            case ShortBuffStatusUpdateOpcode:
                model = ParseShortBuff(reader);
                return model != null;

            case NpcHtmlMessageOpcode:
                model = ParseWireThen<NpcHtmlMessageDto>(reader, 0);
                return model != null;

            case NpcSayOpcode:
                model = ParseNpcSay(reader);
                return model != null;

            case ShowBoardOpcode:
                model = ParseWireThen<ShowCBoardDto>(reader, 0);
                return model != null;

            case AskJoinPartyOpcode:
                model = ParseWireThen<AskJoinPartyDto>(reader, 0);
                return model != null;

            case TradeDoneOpcode:
                reader.ReadI(); // SendTradeDone.java: D response
                model = new TradeDoneIncomingDto();
                return true;

            case TradeStartOpcode:
                model = ParseTradeStart(reader);
                return model != null;

            case PledgeShowMemberUpdateOpcode:
                model = ParsePledgeMemberUpdate(reader);
                return model != null;

            case PledgeShowMemberAllOpcode:
                model = ParsePledgeAll(reader);
                return model != null;

            case PledgeShowMemberAddOpcode:
                model = ParsePledgeMemberAdd(reader);
                return model != null;

            case PledgeShowMemberDeleteOpcode:
                model = ParseWireThen<PledgeShowMemberListDeleteDto>(reader, 0);
                return model != null;

            case PledgeShowMemberDeleteAllOpcode:
                model = new PledgeShowMemberListDeleteAllDto();
                return true;

            case PledgeStatusChangedOpcode:
                model = ParseWireThen<PledgeStatusChangedDto>(reader, 0);
                return model != null;

            case PetInfoOpcode:                model = ParsePetInfo(reader);
                return model != null;

            case DeleteObjectOpcode:
                model = new DeleteObjectDto { ObjectId = reader.ReadI() };
                reader.ReadI(); // D 0 final de Lucera
                return true;

            case NewCharacterSuccessOpcode:
                model = ParseNewCharacterSuccess(reader);
                return model != null;

            case CharacterCreateSuccessOpcode:
                model = CharCreateOkDto.From(reader.ReadI() == 1);
                return true;

            case CharacterCreateFailOpcode:
                model = CharCreateFailDto.From(reader.ReadI());
                return true;

            default:
                return false;
        }
    }

    public void SetGameCryptKey(byte[] key)
    {
        lock (_gameCryptLock)
        {
            _gameCrypt = new L2_login.GameCrypt();
            _gameCrypt.SetKey(key);
        }
    }

    private static InterludeKeyDto ParseKeyPacket(PacketReader reader)
    {
        bool auth = reader.ReadB() != 0;
        long key = reader.ReadLOther();
        reader.ReadI(); // 1
        reader.ReadI(); // 0
        reader.ReadB(); // 0
        reader.ReadI(); // 0
        reader.ReadB(); // mask
        reader.ReadB();
        reader.ReadB();
        InterludeKeyDto dto = InterludeKeyDto.From(auth, BitConverter.GetBytes(key), true, 0);
        ActivateKey(dto);
        return dto;
    }

    private static void ActivateKey(InterludeKeyDto packet)
    {
        if (packet == null || !packet.AuthAllowed)
        {
            return;
        }

        if (IncomingPacketActions.Game == null || IncomingPacketActions.Login == null)
        {
            UnityEngine.Debug.LogWarning("[Lucera] KeyPacket sin Game/Login listos");
            return;
        }

        if (packet.UseBlowfish)
        {
            byte[] equalsKey = BlowFishStaticKey.GetCreateFullKeyBlowFish(packet.BlowFishKey);
            IncomingPacketActions.Game.EnableCrypt(equalsKey);
        }

        IncomingPacketActions.Game.Send(new AuthLoginCommand(
            IncomingPacketActions.Login.Account,
            IncomingPacketActions.Game.PlayKey1,
            IncomingPacketActions.Game.PlayKey2,
            IncomingPacketActions.Game.SessionKey1,
            IncomingPacketActions.Game.SessionKey2));
    }

    private static CharSelectionInfoDto ParseCharacterSelection(PacketReader reader)
    {
        int count = reader.ReadI();
        reader.ReadI(); // n3
        reader.ReadB(); // n22
        reader.ReadB(); // unk1
        reader.ReadI(); // serverType
        reader.ReadB();
        reader.ReadB();

        CharSelectionInfoDto dto = new CharSelectionInfoDto();
        for (int i = 0; i < count; i++)
        {
            CharSelectInfoPackage character = new CharSelectInfoPackage();
            character.Slot = i;
            character.Name = reader.ReadOtherS();
            character.ObjId = reader.ReadI();
            character.Account = reader.ReadOtherS();
            character.AccountId = reader.ReadI();
            character.ClanId = reader.ReadI();
            character.BuildLvl = reader.ReadI();
            character.Sex = reader.ReadI();
            character.Race = reader.ReadI();
            int baseClass = reader.ReadI();
            character.ClassId = baseClass;
            reader.ReadI(); // active
            reader.ReadI(); // x
            reader.ReadI(); // y
            reader.ReadI(); // z
            character.Hp = reader.ReadD();
            character.Mp = reader.ReadD();
            reader.ReadLOther(); // sp
            reader.ReadLOther(); // exp
            reader.ReadD(); // exp percent
            character.Lvl = reader.ReadI();
            character.Karma = reader.ReadI();
            reader.ReadI(); // pk
            reader.ReadI(); // pvp
            for (int pad = 0; pad < 9; pad++) reader.ReadI();
            for (int slot = 0; slot < 60; slot++) reader.ReadI();
            for (int slot = 0; slot < 9; slot++) reader.ReadI();
            for (int enchant = 0; enchant < 5; enchant++) reader.ReadSh();
            reader.ReadI(); // paperdoll item id
            character.HairColor = reader.ReadI();
            character.Face = reader.ReadI();
            character.MaxHp = reader.ReadD();
            character.MaxMp = reader.ReadD();
            reader.ReadI(); // access
            reader.ReadI(); // class id 2
            int selected = reader.ReadI();
            reader.ReadB();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadD();
            reader.ReadD();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadB();
            reader.ReadB();
            reader.ReadB();

            character.Selected = selected == 1;
            CharacterRace race = MapClassId.GetCharacterRace(character.ClassId);
            bool isMage = MapClassId.IsMage(character.ClassId);
            character.CharacterRaceAnimation =
                CharacterRaceAnimationParser.ParseRace(race, (byte)character.Sex, isMage);
            dto.Characters.Add(character);
            if (character.Selected)
            {
                dto.SelectedSlotId = i;
            }
        }

        return dto;
    }

    private static CharTemplatesDto ParseNewCharacterSuccess(PacketReader reader)
    {
        CharTemplatesDto dto = new CharTemplatesDto();
        int size = reader.ReadI();
        for (int i = 0; i < size; i++)
        {
            PlayerTemplates template = new PlayerTemplates();
            template.Race = reader.ReadI();
            template.SetClassId(reader.ReadI());
            reader.ReadI(); // str max
            template.Base_str = reader.ReadI();
            reader.ReadI(); // str min
            reader.ReadI(); // dex max
            template.Base_dex = reader.ReadI();
            reader.ReadI(); // dex min
            reader.ReadI(); // con max
            template.Base_con = reader.ReadI();
            reader.ReadI(); // con min
            reader.ReadI(); // int max
            template.Base_int = reader.ReadI();
            reader.ReadI(); // int min
            reader.ReadI(); // wit max
            template.Base_wit = reader.ReadI();
            reader.ReadI(); // wit min
            reader.ReadI(); // men max
            template.Base_men = reader.ReadI();
            reader.ReadI(); // men min
            dto.PlayerTemplates.Add(template);
        }

        return dto;
    }

    private static int ResolveRace(string raceName)
    {
        switch (raceName)
        {
            case "Elf": return (int)CharacterRace.Elf;
            case "Dark Elf": return (int)CharacterRace.DarkElf;
            case "Orc": return (int)CharacterRace.Orc;
            case "Dwarf": return (int)CharacterRace.Dwarf;
            case "Human":
            default: return (int)CharacterRace.Human;
        }
    }

    private static PlayerTemplates ResolveTemplate(CharacterCreateCommand command)
    {
        if (command.Templates == null || command.Templates.Count == 0)
        {
            throw new NotSupportedException(
                "Lucera 166: CharacterCreate sin plantillas de NewCharacterSuccess.");
        }

        int race = ResolveRace(command.RaceName);
        bool wantMage = command.ClassName == "Mystic";
        for (int i = 0; i < command.Templates.Count; i++)
        {
            PlayerTemplates template = command.Templates[i];
            if (template.Race == race && MapClassId.IsMage(template._classId) == wantMage)
            {
                return template;
            }
        }

        for (int i = 0; i < command.Templates.Count; i++)
        {
            if (command.Templates[i].Race == race)
            {
                return command.Templates[i];
            }
        }

        throw new NotSupportedException(
            "Lucera 166: no hay clase para race=" + command.RaceName + " class=" + command.ClassName);
    }

    private static int ParseAppearance(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        int typeIndex = value.IndexOf("Type ", StringComparison.Ordinal);
        if (typeIndex >= 0 && value.Length > typeIndex + 5)
        {
            return value[typeIndex + 5] - 'A';
        }

        int parsed;
        return int.TryParse(value, out parsed) ? parsed : 0;
    }

    private static T ParseWire<T>(byte[] raw) where T : class, IWireDto, new()
    {
        T dto = new T();
        dto.ReadFrom(new PacketReader(raw));
        return dto;
    }

    /// <summary>DTO compartido con ACIS + campos finales que Lucera agrega y el DTO no lee.</summary>
    private static T ParseWireThen<T>(PacketReader reader, int trailingBytes) where T : class, IWireDto, new()
    {
        T dto = new T();
        dto.ReadFrom(reader);
        if (trailingBytes > 0) reader.ReadB(trailingBytes);
        return dto;
    }

    // ShortCutPacket.java (Lucera): D type, D slot+page*12, y según el tipo:
    // 1 item: 8×D | 2 skill: D id, D level, D reuseGroup, C 0, D charType | resto: D id, D charType.
    private static Shortcut ParseShortcutEntry(PacketReader reader)
    {
        int type = reader.ReadI();
        int worldSlot = reader.ReadI();
        int slot = worldSlot % 12;
        int page = worldSlot / 12;
        int id = reader.ReadI();
        int level = 0;
        if (type == Shortcut.TYPE_ITEM)
        {
            for (int i = 0; i < 7; i++) reader.ReadI();
        }
        else if (type == Shortcut.TYPE_SKILL)
        {
            level = reader.ReadI();
            reader.ReadI(); // reuse group
            reader.ReadB();
            reader.ReadI(); // character type
        }
        else
        {
            reader.ReadI(); // character type
        }

        int clientType = type == Shortcut.TYPE_ITEM || type == Shortcut.TYPE_SKILL ? type : Shortcut.TYPE_ACTION;
        return new Shortcut(slot, page, clientType, id, level);
    }

    private static ShortCutInitDto ParseShortCutInit(PacketReader reader)
    {
        ShortCutInitDto dto = new ShortCutInitDto();
        int count = reader.ReadI();
        for (int i = 0; i < count; i++)
        {
            dto.ShortCuts.Add(ParseShortcutEntry(reader));
        }

        return dto;
    }

    // NpcSay.java (Lucera): D objId, D chatType, D npcId+1000000, D npcStringId, S parámetros...
    private static NpcSayDto ParseNpcSay(PacketReader reader)
    {
        int objectId = reader.ReadI();
        int chatType = reader.ReadI();
        int npcId = reader.ReadI() - 1000000;
        reader.ReadI(); // npc string id (-1 = texto libre)
        string text = string.Empty;
        bool first = true;
        while (reader.Remaining > 0)
        {
            string part = reader.ReadOtherS();
            if (first) { text = part; first = false; }
        }

        return NpcSayDto.From(objectId, chatType, npcId, text);
    }

    private static ShortCutRegisterDto ParseShortCutRegister(PacketReader reader)
    {
        return ShortCutRegisterDto.From(ParseShortcutEntry(reader));
    }

    private static InventoryUpdateDto ParseInventoryUpdate(PacketReader reader)
    {
        InventoryUpdateDto dto = new InventoryUpdateDto();
        reader.ReadB(); // F2
        reader.ReadI(); // size
        int count = reader.ReadI();
        for (int i = 0; i < count; i++)
        {
            int lastChange = reader.ReadSh();
            ItemInstance item = ParseItem(reader);
            dto.AddParsedItem(item, lastChange);
        }

        return dto;
    }

    private static ItemListDto ParseItemList(PacketReader reader)
    {
        ItemListDto dto = new ItemListDto();
        // Lucera 166 (ItemList.java): C sendType; 1 = cabecera (D showWindow, D size) SIN ítems;
        // 2 = D size, D size + los ítems. El cliente recibe primero el 1 y después el 2.
        int sendType = reader.ReadB();
        int itemCount;
        if (sendType == 1)
        {
            bool showWindow = reader.ReadI() != 0;
            dto.SetShowWindow(showWindow);
            reader.ReadI(); // size total
            itemCount = 0;
        }
        else
        {
            dto.SetShowWindow(false);
            reader.ReadI(); // size
            itemCount = reader.ReadI();
        }

        for (int i = 0; i < itemCount; i++)
        {
            ItemInstance item = ParseItem(reader);
            if (item != null)
            {
                dto.AddParsedItem(item);
            }
        }

        int extra = reader.ReadSh();
        if (extra > 0)
        {
            reader.ReadB();
            for (int i = 0; i < extra; i++) reader.ReadI();
        }

        dto.FinishParsedItems();
        return dto;
    }

    private static ItemInstance ParseItem(PacketReader reader)
    {
        byte flags = reader.ReadB();
        int objectId = reader.ReadI();
        int itemId = reader.ReadI();
        byte equipSlot = reader.ReadB();
        long count = reader.ReadLOther();
        byte type2 = reader.ReadB();
        reader.ReadB(); // custom type 1
        int equipped = reader.ReadSh();
        long bodyPart = reader.ReadLOther();
        int enchant = reader.ReadB();
        reader.ReadB(); // custom type 2
        reader.ReadI(); // shadow lifetime
        int temporalLifeTime = reader.ReadI();
        reader.ReadB();
        reader.ReadB();
        reader.ReadB();

        if ((flags & 1) != 0)
        {
            reader.ReadI();
            reader.ReadI();
        }

        if ((flags & 2) != 0)
        {
            for (int i = 0; i < 8; i++) reader.ReadSh();
        }

        if ((flags & 4) != 0)
        {
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
        }

        if ((flags & 8) != 0)
        {
            reader.ReadI();
        }

        if ((flags & 16) != 0)
        {
            int ensoul = reader.ReadB();
            if (ensoul == 1) reader.ReadI();
            else if (ensoul == 2)
            {
                reader.ReadI();
                reader.ReadI();
            }

            int special = reader.ReadB();
            if (special == 1) reader.ReadI();
        }

        bool isEquipped = equipped == 1;
        ItemCategory category = ItemsType.ParceCategory(type2);
        ItemSlot slot = ItemsType.ParceSlot((int)bodyPart);
        ItemLocation location = isEquipped ? ItemLocation.Equipped : ItemLocation.Inventory;
        return new ItemInstance(
            objectId,
            itemId,
            location,
            equipSlot,
            (int)count,
            category,
            isEquipped,
            slot,
            enchant,
            temporalLifeTime);
    }

    private static StatusUpdateDto ParseStatusUpdate(PacketReader reader)
    {
        StatusUpdateDto dto = new StatusUpdateDto();
        dto.ObjectId = reader.ReadI();
        reader.ReadI(); // attacker object id
        reader.ReadB(); // hp/mp/cp flag
        int count = reader.ReadB();
        dto.Attributes = new List<StatusUpdate.Attribute>(count);
        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadB();
            int value = reader.ReadI();
            dto.Attributes.Add(new StatusUpdate.Attribute(id, value));
        }

        return dto;
    }

    private static SkillListDto ParseSkillList(PacketReader reader)
    {
        SkillListDto dto = new SkillListDto();
        int count = reader.ReadI();
        for (int i = 0; i < count; i++)
        {
            bool passive = reader.ReadI() == 1;
            int level = reader.ReadSh();
            reader.ReadSh(); // sub level
            int id = reader.ReadI();
            reader.ReadI(); // reuse delay group
            bool disabled = reader.ReadB() == 1;
            reader.ReadB(); // enchanted
            dto.Skills.Add(new SkillInstance(id, level, passive, disabled));
        }

        reader.ReadI(); // skill recién aprendida (SkillList.java, último writeD)
        return dto;
    }

    private static AbnormalStatusUpdateDto ParseAbnormalStatus(PacketReader reader)
    {
        AbnormalStatusUpdateDto dto = new AbnormalStatusUpdateDto();
        int count = reader.ReadSh();
        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadI();
            int level = reader.ReadSh();
            reader.ReadI(); // client abnormal id
            int duration = reader.ReadSh();
            if (duration == 0x7FFF)
            {
                duration = reader.ReadI();
            }
            dto.ListEffect.Add(new EffectHolder(id, level, duration));
        }

        return dto;
    }

    private static AcquireSkillListDto ParseAcquireSkillList(PacketReader reader)
    {
        AcquireSkillListDto dto = new AcquireSkillListDto();
        int count = reader.ReadSh();
        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadI();
            int level = reader.ReadSh();
            long cost = reader.ReadLOther();
            int minLevel = reader.ReadB();
            reader.ReadSh(); // unknown
            int hasItem = reader.ReadB();
            if (hasItem == 1)
            {
                reader.ReadI(); // item id
                reader.ReadLOther(); // item count
            }

            reader.ReadB(); // unknown
            dto.AcquireList.Add(new OtherModel(
                new AcquireData(id, level, minLevel, (int)cost, (int)AcquireSkillType.USUAL)));
        }

        return dto;
    }

    private static AcquireSkillInfoDto ParseAcquireSkillInfo(PacketReader reader)
    {
        int id = reader.ReadI();
        int level = reader.ReadI();
        long cost = reader.ReadLOther();
        reader.ReadI(); // id 2
        int count = reader.ReadI();
        List<RequiredSkillInfo> required = new List<RequiredSkillInfo>(count);
        for (int i = 0; i < count; i++)
        {
            int type = reader.ReadI();
            int itemId = reader.ReadI();
            long itemCount = reader.ReadLOther();
            int unk = reader.ReadI();
            required.Add(new RequiredSkillInfo(type, itemId, (int)itemCount, unk));
        }

        return AcquireSkillInfoDto.From(id, level, (int)cost, 0, required);
    }

    private static CreatureSayDto ParseSay2(PacketReader reader)
    {
        // Say2.java: ordinales de ChatType -> TELL=2, CLAN=4, ALLIANCE=9, SYSTEM_MESSAGE=11.
        int objectId = reader.ReadI();
        int chatType = reader.ReadI();
        if (chatType == 11)
        {
            reader.ReadI(); // id del mensaje de sistema
            reader.ReadI();
            return CreatureSayDto.From(objectId, chatType, string.Empty, string.Empty);
        }

        string sender = reader.ReadOtherS();
        reader.ReadI(); // npc string id
        string text = string.Empty;
        if (chatType == 2)
        {
            text = reader.ReadOtherS();
            int flags = reader.ReadB();
            if ((flags & 0x10) == 0) reader.ReadB();
            return CreatureSayDto.From(objectId, chatType, sender, text);
        }

        int tail = chatType == 4 || chatType == 9 ? 1 : 0; // CLAN/ALLIANCE terminan con C 0
        bool first = true;
        while (reader.Remaining > tail)
        {
            string part = reader.ReadOtherS();
            if (first) { text = part; first = false; }
        }

        if (tail == 1) reader.ReadB();
        return CreatureSayDto.From(objectId, chatType, sender, text);
    }

    private static WarehouseDepositListDto ParseWarehouseDeposit(PacketReader reader)
    {
        int variant = reader.ReadB();
        int whType = 0;
        int adena = 0;
        int count;
        if (variant == 1)
        {
            whType = reader.ReadSh();
            adena = (int)reader.ReadLOther();
            reader.ReadI();
            reader.ReadSh();
            count = reader.ReadI();
        }
        else
        {
            reader.ReadI();
            count = reader.ReadI();
        }

        List<Product> products = new List<Product>(count);
        for (int i = 0; i < count; i++)
        {
            ItemInstance item = ParseItem(reader);
            reader.ReadI();
            if (item != null)
            {
                products.Add(new Product(0, item.ObjectId, item.Count,
                    (int)item.Category, 0, (int)item.BodyPart, item.EnchantLevel, 0, item.ItemId));
            }
        }

        return WarehouseDepositListDto.From(whType, adena, products);
    }

    private static WarehouseWithdrawListDto ParseWarehouseWithdraw(PacketReader reader)
    {
        int variant = reader.ReadB();
        int whType = 0;
        int adena = 0;
        int count;
        if (variant == 1)
        {
            whType = reader.ReadSh();
            adena = (int)reader.ReadLOther();
            reader.ReadI();
            count = reader.ReadI();
        }
        else
        {
            if (variant != 4) reader.ReadSh();
            reader.ReadI();
            count = reader.ReadI();
        }

        List<Product> products = new List<Product>(count);
        for (int i = 0; i < count; i++)
        {
            ItemInstance item = ParseItem(reader);
            reader.ReadI();
            if (item != null)
            {
                products.Add(new Product(0, item.ObjectId, item.Count,
                    (int)item.Category, 0, (int)item.BodyPart, item.EnchantLevel, 0, item.ItemId));
            }
        }

        return WarehouseWithdrawListDto.From(whType, adena, products);
    }

    private static PackageSendableListDto ParsePackageSendable(PacketReader reader)
    {
        int variant = reader.ReadB();
        int playerObject;
        int adena = 0;
        int count;
        if (variant == 2)
        {
            playerObject = reader.ReadI();
            count = reader.ReadI();
        }
        else
        {
            playerObject = reader.ReadI();
            adena = (int)reader.ReadLOther();
            reader.ReadI();
            count = 0;
        }

        List<Product> products = new List<Product>(count);
        for (int i = 0; i < count; i++)
        {
            ItemInstance item = ParseItem(reader);
            reader.ReadI();
            if (item != null)
            {
                products.Add(new Product(0, item.ObjectId, item.Count,
                    (int)item.Category, 0, (int)item.BodyPart, item.EnchantLevel, 0, item.ItemId));
            }
        }

        return PackageSendableListDto.From(playerObject, adena, products);
    }

    private static MultiSellListDto ParseMultiSellList(PacketReader reader)
    {
        reader.ReadB();
        int listId = reader.ReadI();
        reader.ReadB();
        int page = reader.ReadI();
        int pageSize = reader.ReadI();
        reader.ReadI(); // MULTISELL_SIZE
        int count = reader.ReadI();
        reader.ReadB();
        reader.ReadB();
        reader.ReadI();
        MultiSellListDto dto = new MultiSellListDto(listId, page, pageSize, 0);
        for (int e = 0; e < count; e++)
        {
            reader.ReadI(); // entry id
            reader.ReadB();
            reader.ReadSh();
            reader.ReadI();
            reader.ReadI();
            ReadMultiSellElements(reader);
            reader.ReadB();
            reader.ReadB();
            int productCount = reader.ReadSh();
            int ingredientCount = reader.ReadSh();
            List<ItemInstance> products = new List<ItemInstance>(productCount);
            for (int i = 0; i < productCount; i++)
            {
                int itemId = reader.ReadI();
                reader.ReadLOther(); // body part
                int type2 = reader.ReadSh();
                int itemCount = (int)reader.ReadLOther();
                int enchant = reader.ReadSh();
                reader.ReadI();
                reader.ReadI();
                reader.ReadI();
                ReadMultiSellElements(reader);
                reader.ReadB();
                reader.ReadB();
                products.Add(new ItemInstance(0, itemId, ItemLocation.Trade, i, itemCount,
                    ItemsType.ParceCategory(type2), false, ItemSlot.none, enchant, 9999));
            }

            List<Ingredient> ingredients = new List<Ingredient>(ingredientCount);
            for (int i = 0; i < ingredientCount; i++)
            {
                int itemId = reader.ReadI();
                int type2 = reader.ReadSh();
                int itemCount = (int)reader.ReadLOther();
                int enchant = reader.ReadSh();
                reader.ReadI();
                reader.ReadI();
                ReadMultiSellElements(reader);
                reader.ReadB();
                reader.ReadB();
                ingredients.Add(new Ingredient(itemId, type2, itemCount, enchant));
            }

            dto.AddEntry(new MultiSellData(products, ingredients));
        }

        return dto;
    }

    private static void ReadMultiSellElements(PacketReader reader)
    {
        for (int i = 0; i < 8; i++) reader.ReadSh();
    }

    private static BuyListSeedDto ParseBuyListSeed(PacketReader reader)
    {
        int money = (int)reader.ReadLOther();
        int manorId = reader.ReadI();
        reader.ReadI();
        int count = reader.ReadSh();
        List<Product> products = new List<Product>(count);
        for (int i = 0; i < count; i++)
        {
            ItemInstance item = ParseItem(reader);
            long ownersPrice = reader.ReadLOther();
            if (item != null)
            {
                products.Add(new Product(0, item.ObjectId, item.Count,
                    (int)item.Category, 0, (int)item.BodyPart, item.EnchantLevel,
                    (int)ownersPrice, item.ItemId));
            }
        }

        return BuyListSeedDto.From(manorId, money, products);
    }

    private static ShopPreviewListDto ParseShopPreviewList(PacketReader reader)
    {
        int listId = reader.ReadI();
        reader.ReadLOther();
        int money = reader.ReadI();
        int count = reader.ReadSh();
        List<Product> products = new List<Product>(count);
        for (int i = 0; i < count; i++)
        {
            int itemId = reader.ReadI();
            int type2 = reader.ReadSh();
            reader.ReadLOther();
            long price = reader.ReadLOther();
            products.Add(new Product(0, 0, 1, type2, 0, 0, 0, (int)price, itemId));
        }

        return ShopPreviewListDto.From(money, listId, products);
    }

    private static DieDto ParseDie(PacketReader reader)
    {
        int objectId = reader.ReadI();
        int toVillage = reader.ReadI();
        reader.ReadI(); // clanhall
        reader.ReadI(); // castle
        reader.ReadI(); // flag
        int sweep = reader.ReadI();
        int fixedRes = reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadB();
        reader.ReadI();
        reader.ReadI();
        return DieDto.From(objectId, toVillage != 0, sweep != 0, fixedRes != 0);
    }

    private static SpawnItemDto ParseSpawnItem(PacketReader reader)
    {
        int objectId = reader.ReadI();
        int itemId = reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        int stackable = reader.ReadI();
        int count = (int)reader.ReadLOther();
        int unknown = reader.ReadI();
        reader.ReadB();
        reader.ReadB();
        reader.ReadB();
        return SpawnItemDto.From(objectId, itemId, x, y, z, stackable != 0, count, unknown);
    }

    private static DropItemDto ParseDropItem(PacketReader reader)
    {
        int charObjId = reader.ReadI();
        int objectId = reader.ReadI();
        int itemId = reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        int stackable = reader.ReadB();
        int count = (int)reader.ReadLOther();
        int unknown = reader.ReadB();
        reader.ReadB();
        reader.ReadB();
        reader.ReadB();
        return DropItemDto.From(charObjId, objectId, itemId, x, y, z, stackable != 0, count, unknown);
    }

    private static GetItemDto ParseGetItem(PacketReader reader)
    {
        int playerId = reader.ReadI();
        int objectId = reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        return GetItemDto.From(playerId, objectId, x, y, z);
    }

    private static CharInfoDto ParseCharInfo(PacketReader reader)
    {
        CharInfoDto dto = new CharInfoDto();
        reader.ReadB(); // unk0
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        dto.Identity.SetL2jPos(x, y, z);
        reader.ReadI(); // boat object id
        dto.Identity.Id = reader.ReadI();
        dto.Identity.Name = reader.ReadOtherS();
        int race = reader.ReadSh();
        int packetRace = race;
        dto.Appearance.Race = (int)MapClassId.GetRace(race);
        dto.Appearance.Sex = reader.ReadB();
        int classId = reader.ReadI();
        int packetClassFirst = classId;
        int packetClassSecond = classId;
        dto.Appearance.BaseClass = classId;
        dto.Identity.PlayerClass = classId;

        for (int i = 0; i < 12; i++) reader.ReadI();
        for (int i = 0; i < 3; i++)
        {
            reader.ReadI();
            reader.ReadI();
        }
        reader.ReadB(); // armor set enchant
        for (int i = 0; i < 9; i++) reader.ReadI();

        reader.ReadB(); // pvp
        dto.Stats.Karma = reader.ReadI();
        dto.Stats.MAtkSpd = reader.ReadI();
        dto.Stats.PAtkSpd = reader.ReadI();
        dto.Stats.BaseRunSpeed = reader.ReadSh();
        dto.Stats.BaseWalkingSpeed = reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        dto.Stats.Speed = dto.Stats.BaseRunSpeed;
        double moveMod = reader.ReadD();
        reader.ReadD(); // attack speed multiplier
        dto.Stats.RunRealSpeed = dto.Stats.BaseRunSpeed * (float)moveMod;
        dto.Stats.WalkRealSpeed = dto.Stats.BaseWalkingSpeed * (float)moveMod;
        dto.Appearance.CollisionRadius = (float)reader.ReadD();
        dto.Appearance.CollisionHeight = (float)reader.ReadD();

        dto.Appearance.HairStyle = reader.ReadI();
        dto.Appearance.HairColor = reader.ReadI();
        dto.Appearance.Face = reader.ReadI();
        dto.Identity.Title = reader.ReadOtherS();
        dto.Identity.ClanId = reader.ReadI();
        reader.ReadI(); // clan crest
        reader.ReadI(); // ally
        reader.ReadI(); // ally crest
        reader.ReadB(); // sitting
        dto.Identity.IsRunning = reader.ReadB() == 1;
        dto.Appearance.Running = dto.Identity.IsRunning;
        reader.ReadB(); // combat
        bool alikeDead = reader.ReadB() == 1;
        reader.ReadB(); // store
        byte mount = reader.ReadB();
        reader.ReadB(); // private store
        int cubics = reader.ReadSh();
        for (int i = 0; i < cubics; i++) reader.ReadSh();
        reader.ReadB(); // party leader
        reader.ReadB(); // flying
        reader.ReadSh(); // recom
        reader.ReadI(); // mount npc
        int class2 = reader.ReadI();
        packetClassSecond = class2;
        reader.ReadI();
        reader.ReadB();
        reader.ReadB();
        reader.ReadI();
        reader.ReadB();
        reader.ReadB();
        reader.ReadB();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadB();
        reader.ReadSh();
        reader.ReadI();
        reader.ReadB();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadB();
        dto.Status.Cp = reader.ReadI();
        dto.Stats.MaxHp = reader.ReadI();
        dto.Status.SetHp(reader.ReadI());
        dto.Stats.MaxMp = reader.ReadI();
        dto.Status.SetMp(reader.ReadI());
        reader.ReadB();
        int extra = reader.ReadI();
        for (int i = 0; i < extra; i++) reader.ReadSh();
        reader.ReadB();
        reader.ReadB();
        reader.ReadB();

        dto.SetPacketMeta(packetRace, packetClassFirst, packetClassSecond, alikeDead, mount);
        return dto;
    }

    private static AttackDto ParseAttack(PacketReader reader)
    {
        int attacker = reader.ReadI();
        int target = reader.ReadI();
        reader.ReadI(); // F2
        int damage = reader.ReadI();
        int flags = reader.ReadI();
        reader.ReadI(); // soulshot
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        int extra = reader.ReadSh();
        UnityEngine.Vector3 attackerPos = VectorUtils.ConvertPosToUnity(new UnityEngine.Vector3(x, y, z));
        Hit[] hits = new Hit[extra + 1];
        hits[0] = new Hit(target, damage, flags);
        for (int i = 0; i < extra; i++)
        {
            int hitTarget = reader.ReadI();
            int hitDamage = reader.ReadI();
            int hitFlags = reader.ReadI();
            reader.ReadI(); // soulshot
            hits[i + 1] = new Hit(hitTarget, hitDamage, hitFlags);
        }

        int tx = reader.ReadI();
        int ty = reader.ReadI();
        int tz = reader.ReadI();
        return AttackDto.From(
            attacker, target, damage, flags, attackerPos,
            VectorUtils.ConvertPosToUnity(new UnityEngine.Vector3(tx, ty, tz)), hits);
    }

    private static MyTargetSelectDto ParseTargetSelected(PacketReader reader)
    {
        reader.ReadI(); // F2
        int target = reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        return MyTargetSelectDto.From(target, 0);
    }

    private static MagicSkillUseDto ParseMagicSkillUse(PacketReader reader)
    {
        reader.ReadI(); // unknown
        int caster = reader.ReadI();
        int target = reader.ReadI();
        int skillId = reader.ReadI();
        int skillLevel = reader.ReadI();
        int hitTime = reader.ReadI();
        reader.ReadI(); // unknown
        int reuse = reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        reader.ReadSh();
        reader.ReadSh();
        int targetX = reader.ReadI();
        int targetY = reader.ReadI();
        int targetZ = reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        return MagicSkillUseDto.From(
            caster, target, skillId, skillLevel, hitTime, reuse,
            VectorUtils.ConvertPosToUnity(new UnityEngine.Vector3(x, y, z)),
            VectorUtils.ConvertPosToUnity(new UnityEngine.Vector3(targetX, targetY, targetZ)));
    }

    private static MagicSkillLaunchedDto ParseMagicSkillLaunched(PacketReader reader)
    {
        reader.ReadI(); // unknown
        int caster = reader.ReadI();
        int skillId = reader.ReadI();
        int skillLevel = reader.ReadI();
        int count = reader.ReadI();
        int[] targets = new int[count];
        for (int i = 0; i < count; i++)
        {
            targets[i] = reader.ReadI();
        }

        return MagicSkillLaunchedDto.From(caster, skillId, skillLevel, targets);
    }

    private static TradeStartDto ParseTradeStart(PacketReader reader)
    {
        int variant = reader.ReadB();
        if (variant == 1)
        {
            int player = reader.ReadI();
            reader.ReadB();
            reader.ReadB();
            reader.ReadB();
            reader.ReadSh();
            reader.ReadB();
            return TradeStartDto.From(player, new List<ItemInstance>());
        }

        int playerId = reader.ReadI();
        int count = reader.ReadSh();
        reader.ReadB();
        reader.ReadB();
        List<ItemInstance> items = new List<ItemInstance>(count);
        for (int i = 0; i < count; i++)
        {
            ItemInstance item = ParseItem(reader);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return TradeStartDto.From(playerId, items);
    }

    private static PledgeShowMemberListAllDto ParsePledgeAll(PacketReader reader)
    {
        int isMainClan = reader.ReadI();
        int clanId = reader.ReadI();
        int requestId = reader.ReadI();
        int subUnitId = reader.ReadI();
        string subUnitName = reader.ReadOtherS();
        string leaderName = reader.ReadOtherS();
        int crestId = reader.ReadI();
        int level = reader.ReadI();
        int castle = reader.ReadI();
        reader.ReadI(); // unknown
        int hideout = reader.ReadI();
        reader.ReadI(); // unknown
        int rank = reader.ReadI();
        int reputation = reader.ReadI();
        int disband = reader.ReadI();
        reader.ReadI(); // unknown
        int allyId = reader.ReadI();
        string allyName = reader.ReadOtherS();
        int allyCrest = reader.ReadI();
        int war = reader.ReadI();
        reader.ReadI(); // unknown
        int count = reader.ReadI();
        List<ClanMember> members = new List<ClanMember>(count);
        for (int i = 0; i < count; i++)
        {
            string name = reader.ReadOtherS();
            int memberLevel = reader.ReadI();
            int classId = reader.ReadI();
            int sex = reader.ReadI();
            int race = reader.ReadI();
            int online = reader.ReadI();
            int sponsor = reader.ReadI();
            members.Add(new ClanMember(name, memberLevel, classId, sex, race, online, sponsor));
        }

        return PledgeShowMemberListAllDto.From(
            isMainClan, clanId, subUnitId, subUnitName, leaderName, requestId,
            crestId, level, castle, hideout, rank, reputation, disband,
            allyId, allyName, allyCrest, war == 1, members);
    }

    private static PledgeShowMemberListUpdateDto ParsePledgeMemberUpdate(PacketReader reader)
    {
        string name = reader.ReadOtherS();
        int level = reader.ReadI();
        int classId = reader.ReadI();
        int sex = reader.ReadI();
        reader.ReadI(); // object id
        int online = reader.ReadI();
        int pledgeType = reader.ReadI();
        int sponsor = reader.ReadI();
        return PledgeShowMemberListUpdateDto.From(name, level, classId, sex, 0, online, pledgeType, sponsor);
    }

    private static PledgeShowMemberListAddDto ParsePledgeMemberAdd(PacketReader reader)
    {
        string name = reader.ReadOtherS();
        int level = reader.ReadI();
        int classId = reader.ReadI();
        int online = reader.ReadI();
        int pledgeType = reader.ReadI();
        int race = reader.ReadI();
        int sex = reader.ReadI();
        return PledgeShowMemberListAddDto.From(name, level, classId, online, pledgeType, race, sex);
    }

    // PetInfo.java (Lucera 166). Llena los mismos campos que NpcInfoDto.ReadPet (layout Interlude),
    // con la secuencia del server: C type, D objId, D npcId+1000000, D x,y,z,h, D mAtkSpd, D pAtkSpd,
    // H×8 velocidades, F(double)×4, D×3, C, D, S name, D, S title, C pvp, D karma, D fed, D maxFed,
    // D hp, D maxHp, D mp, D maxMp, Q sp, C level, Q×3 exp, D×16, C×4, D, D, C, C, H n + n×H, C.
    private static NpcInfoDto ParsePetInfo(PacketReader reader)
    {
        NpcInfoDto dto = new NpcInfoDto();
        dto.Identity.SummonState = reader.ReadB(); // tipo de summon
        dto.Identity.Id = reader.ReadI();
        dto.Identity.NpcId = reader.ReadI() - 1000000;
        dto.Identity.Owned = true;
        dto.Identity.SetHideHp(dto.Identity.NpcId);
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        dto.Identity.SetL2jPos(x, y, z);
        float heading = reader.ReadI();
        dto.Identity.OrigHeading = VectorUtils.HeadingToUnityQuaternionForNpc(heading);
        dto.Identity.Heading = UnityEngine.Quaternion.Euler(0, dto.Identity.OrigHeading, 0);
        dto.Stats.MAtkSpd = reader.ReadI();
        dto.Stats.PAtkSpd = reader.ReadI();
        dto.Stats.BaseRunSpeed = reader.ReadSh();
        dto.Stats.BaseWalkingSpeed = reader.ReadSh();
        for (int i = 0; i < 6; i++) reader.ReadSh(); // swim / fl / fly
        float moveMultiplier = (float)reader.ReadD();
        reader.ReadD(); // atk speed multiplier
        dto.Stats.WalkRealSpeed = dto.Stats.BaseWalkingSpeed * moveMultiplier;
        dto.Stats.RunRealSpeed = dto.Stats.BaseRunSpeed * moveMultiplier;
        dto.Appearance.CollisionRadius = (float)reader.ReadD();
        dto.Appearance.CollisionHeight = (float)reader.ReadD();
        reader.ReadI(); // rhand
        reader.ReadI(); // chest
        reader.ReadI(); // lhand
        reader.ReadB(); // show spawn animation
        reader.ReadI(); // npc string name
        dto.Identity.IsRunning = true;
        dto.Identity.Name = reader.ReadOtherS();
        reader.ReadI(); // npc string title
        dto.Identity.Title = reader.ReadOtherS();
        reader.ReadB(); // pvp flag
        reader.ReadI(); // karma
        reader.ReadI(); // fed
        reader.ReadI(); // max fed
        int hp = reader.ReadI();
        dto.Stats.MaxHp = reader.ReadI();
        int mp = reader.ReadI();
        dto.Stats.MaxMp = reader.ReadI();
        dto.Status.SetHp(hp);
        dto.Status.SetMp(mp);
        reader.ReadLOther(); // sp
        dto.Stats.Level = reader.ReadB();
        reader.ReadLOther(); // exp
        reader.ReadLOther(); // exp this level
        reader.ReadLOther(); // exp next level
        for (int i = 0; i < 16; i++) reader.ReadI(); // peso, stats de combate, velocidades
        for (int i = 0; i < 4; i++) reader.ReadB(); // enchant effect, team, soulshots, spiritshots
        reader.ReadI(); // form
        reader.ReadI();
        reader.ReadB();
        reader.ReadB();
        int abnormal = reader.ReadSh();
        for (int i = 0; i < abnormal; i++) reader.ReadSh();
        reader.ReadB(); // state flags
        return dto;
    }

    private static PledgeInfoDto ParsePledgeInfo(PacketReader reader)
    {
        reader.ReadI(); // request id
        int clanId = reader.ReadI();
        string clanName = reader.ReadOtherS();
        string allyName = reader.ReadOtherS();
        return PledgeInfoDto.From(clanId, clanName, allyName);
    }

    private static MyTargetSelectDto ParseMyTargetSelected(PacketReader reader)
    {
        reader.ReadI(); // unknown
        int targetId = reader.ReadI();
        int color = reader.ReadSh();
        reader.ReadI(); // unknown
        return MyTargetSelectDto.From(targetId, color);
    }

    private static EtcStatusUpdateDto ParseEtcStatusUpdate(PacketReader reader)
    {
        EtcStatusUpdateDto dto = new EtcStatusUpdateDto();
        reader.ReadB(); // charges
        dto.WeightPenalty[1] = reader.ReadI();
        reader.ReadB(); // weapon grade
        reader.ReadB(); // armor grade
        dto.DeathPenalty[1] = reader.ReadB();
        reader.ReadB(); // souls
        dto.ChatBanned = reader.ReadB();
        return dto;
    }

    private static ShortBuffStatusUpdateDto ParseShortBuff(PacketReader reader)
    {
        int id = reader.ReadI();
        int level = reader.ReadSh();
        reader.ReadSh(); // sub level
        int duration = reader.ReadI();
        return ShortBuffStatusUpdateDto.From(id, level, duration);
    }

    private static SystemMessageDto ParseSystemMessage(PacketReader reader)
    {
        int id = reader.ReadSh();
        int count = reader.ReadB();
        SMParam[] parameters = new SMParam[count];
        for (int i = 0; i < count; i++)
        {
            int type = reader.ReadB();
            SMParam param = new SMParam((SMParam.SMParamType)type);
            switch (type)
            {
                case 0:
                case 12:
                    param.SetValue(reader.ReadOtherS());
                    break;
                case 4:
                    param.SetValue(new[] { reader.ReadI(), reader.ReadSh() });
                    break;
                case 6:
                    param.SetValue(reader.ReadLOther());
                    break;
                case 7:
                case 16:
                    param.SetValue(new[]
                    {
                        (float)reader.ReadI(),
                        (float)reader.ReadI(),
                        (float)reader.ReadI()
                    });
                    break;
                case 8:
                    param.SetValue(new[] { reader.ReadI(), reader.ReadSh(), reader.ReadSh() });
                    break;
                case 15:
                    param.SetValue(reader.ReadSh());
                    break;
                case 20:
                    param.SetValue(reader.ReadB());
                    break;
                default:
                    param.SetValue(reader.ReadI());
                    break;
            }

            parameters[i] = param;
        }

        return SystemMessageDto.From(id, parameters);
    }

    private static UserInfoDto ParseUserInfo(PacketReader reader)
    {
        PlayerInfoInterlude info = new PlayerInfoInterlude
        {
            Identity = new EntityIdentity(),
            Status = new PlayerStatus(),
            Stats = new PlayerStats(),
            Appearance = new PlayerAppearance(),
        };

        info.Identity.Id = reader.ReadI();
        reader.ReadI(); // data size
        reader.ReadSh(); // mask count
        byte[] mask = reader.ReadB(3);

        if (MaskBit(mask, 0, 0x80)) reader.ReadI(); // relation

        if (MaskBit(mask, 0, 0x40))
        {
            reader.ReadSh(); // block length
            info.Identity.Name = ReadSizedString(reader);
            reader.ReadB(); // is gm
            info.Appearance.Race = reader.ReadB();
            info.Appearance.Sex = reader.ReadB();
            info.Appearance.BaseClass = reader.ReadI();
            info.Identity.PlayerClass = reader.ReadI();
            info.Stats.Level = reader.ReadB();
        }

        if (MaskBit(mask, 0, 0x20))
        {
            reader.ReadSh();
            info.Stats.Str = reader.ReadSh();
            info.Stats.Dex = reader.ReadSh();
            info.Stats.Con = reader.ReadSh();
            info.Stats.Int = reader.ReadSh();
            info.Stats.Wit = reader.ReadSh();
            info.Stats.Men = reader.ReadSh();
            reader.ReadSh(); // LUC
            reader.ReadSh(); // CHA
        }

        if (MaskBit(mask, 0, 0x10))
        {
            reader.ReadSh();
            info.Stats.MaxHp = reader.ReadI();
            info.Stats.MaxMp = reader.ReadI();
            info.Stats.MaxCp = reader.ReadI();
        }

        if (MaskBit(mask, 0, 0x08))
        {
            reader.ReadSh();
            info.Status.SetHp(reader.ReadI());
            info.Status.SetMp(reader.ReadI());
            info.Status.Cp = reader.ReadI();
            info.Stats.Sp = reader.ReadLOther();
            info.Stats.Exp = reader.ReadLOther();
            reader.ReadD(); // exp percent
            info.Stats.MaxExp = LevelServer.GetExp(info.Stats.Level + 1);
        }

        if (MaskBit(mask, 0, 0x04))
        {
            reader.ReadSh();
            reader.ReadB(); // weapon enchant
            reader.ReadB(); // armor enchant
        }

        if (MaskBit(mask, 0, 0x02))
        {
            reader.ReadSh();
            info.Appearance.HairStyle = reader.ReadI();
            info.Appearance.HairColor = reader.ReadI();
            info.Appearance.Face = reader.ReadI();
            reader.ReadB(); // hide head accessories
        }

        if (MaskBit(mask, 0, 0x01))
        {
            reader.ReadSh();
            reader.ReadB(); // mount
            reader.ReadB(); // private store
            reader.ReadB(); // dwarf craft
            reader.ReadB();
        }

        if (MaskBit(mask, 1, 0x80))
        {
            reader.ReadSh();
            reader.ReadSh(); // weapon bonus
            info.Stats.PAtk = reader.ReadI();
            info.Stats.PAtkSpd = reader.ReadI();
            info.Stats.PDef = reader.ReadI();
            info.Stats.PEvasion = reader.ReadI();
            info.Stats.PAccuracy = reader.ReadI();
            info.Stats.PCritical = reader.ReadI();
            info.Stats.MAtk = reader.ReadI();
            info.Stats.MAtkSpd = reader.ReadI();
            reader.ReadI(); // p atk spd copy
            reader.ReadI();
            info.Stats.MDef = reader.ReadI();
            reader.ReadI();
            reader.ReadI();
        }

        if (MaskBit(mask, 1, 0x40))
        {
            reader.ReadSh();
            for (int i = 0; i < 6; i++) reader.ReadSh();
        }

        if (MaskBit(mask, 1, 0x20))
        {
            reader.ReadSh();
            int x = reader.ReadI();
            int y = reader.ReadI();
            int z = reader.ReadI();
            info.Identity.SetL2jPos(x, y, z);
            reader.ReadI(); // vehicle
        }

        if (MaskBit(mask, 1, 0x10))
        {
            reader.ReadSh();
            info.Stats.BaseRunSpeed = reader.ReadSh();
            info.Stats.BaseWalkingSpeed = reader.ReadSh();
            reader.ReadSh(); // swim run
            reader.ReadSh(); // swim walk
            reader.ReadSh(); // fly run
            reader.ReadSh(); // fly walk
            reader.ReadSh(); // fly run 2
            reader.ReadSh(); // fly walk 2
            info.Stats.Speed = info.Stats.BaseRunSpeed;
        }

        if (MaskBit(mask, 1, 0x08))
        {
            reader.ReadSh();
            double moveMod = reader.ReadD();
            double atkMod = reader.ReadD();
            info.Stats.RunRealSpeed = info.Stats.BaseRunSpeed * (float)moveMod;
            info.Stats.WalkRealSpeed = info.Stats.BaseWalkingSpeed * (float)moveMod;
            info.Stats.PAtkRealSpeed = info.Stats.PAtkSpd * (float)atkMod;
        }

        if (MaskBit(mask, 1, 0x04))
        {
            reader.ReadSh();
            info.Appearance.CollisionRadius = (float)reader.ReadD();
            info.Appearance.CollisionHeight = (float)reader.ReadD();
        }

        if (MaskBit(mask, 1, 0x02))
        {
            reader.ReadSh();
            reader.ReadB();
            reader.ReadSh();
        }

        if (MaskBit(mask, 1, 0x01))
        {
            reader.ReadSh();
            info.Identity.Title = ReadSizedString(reader);
            reader.ReadSh(); // pledge type
            info.Identity.ClanId = reader.ReadI();
            reader.ReadI(); // clan crest large
            reader.ReadI(); // clan crest
            reader.ReadI(); // clan privileges
            reader.ReadB(); // leader
            reader.ReadI(); // ally
            reader.ReadI(); // ally crest
            reader.ReadB(); // party room leader
        }

        if (MaskBit(mask, 2, 0x80))
        {
            reader.ReadSh();
            reader.ReadB(); // pvp flag
            info.Stats.Karma = reader.ReadI();
            reader.ReadB(); // noble
            reader.ReadB(); // hero
            reader.ReadB(); // pledge class
            info.Stats.PkKills = reader.ReadI();
            info.Stats.PvpKills = reader.ReadI();
            reader.ReadSh();
            reader.ReadSh();
        }

        if (MaskBit(mask, 2, 0x40))
        {
            reader.ReadSh();
            reader.ReadI(); // vitality
            reader.ReadB();
            reader.ReadI(); // fame
            reader.ReadI(); // raid points
        }

        if (MaskBit(mask, 2, 0x20))
        {
            reader.ReadSh();
            reader.ReadB();
            reader.ReadB();
            reader.ReadB();
            reader.ReadI();
            reader.ReadB();
            reader.ReadB();
            reader.ReadB();
        }

        if (MaskBit(mask, 2, 0x10))
        {
            reader.ReadSh();
            reader.ReadB(); // sitting
            info.Identity.IsRunning = reader.ReadB() == 1;
        }

        if (MaskBit(mask, 2, 0x08))
        {
            reader.ReadSh();
            reader.ReadI(); // name color
            reader.ReadI(); // title color
        }

        if (MaskBit(mask, 2, 0x04))
        {
            reader.ReadSh();
            reader.ReadSh();
            reader.ReadSh();
            reader.ReadSh();
            reader.ReadB();
        }

        if (MaskBit(mask, 2, 0x02))
        {
            reader.ReadSh();
            reader.ReadI();
            reader.ReadSh();
            reader.ReadB();
        }

        if (MaskBit(mask, 2, 0x01))
        {
            reader.ReadSh();
            for (int i = 0; i < 6; i++) reader.ReadI();
        }

        return UserInfoDto.From(info);
    }

    private static string ReadSizedString(PacketReader reader)
    {
        int length = reader.ReadSh();
        if (length <= 0)
        {
            return string.Empty;
        }

        return Encoding.Unicode.GetString(reader.ReadB(length * 2));
    }

    private static NpcInfoDto ParseNpcInfo(PacketReader reader)
    {
        NpcInfoDto dto = new NpcInfoDto();
        dto.Identity.Id = reader.ReadI();
        reader.ReadB(); // show spawn animation
        reader.ReadSh(); // mask count
        byte[] mask = reader.ReadB(5);
        reader.ReadB(); // first group block size

        if (MaskBit(mask, 0, 0x40)) reader.ReadB(); // attackable
        if (MaskBit(mask, 0, 0x20)) reader.ReadI(); // unknown
        if (MaskBit(mask, 3, 0x02)) dto.Identity.Title = reader.ReadOtherS();
        reader.ReadSh(); // second group block size

        if (MaskBit(mask, 0, 0x80))
        {
            dto.Identity.NpcId = reader.ReadI() - 1000000;
            dto.Identity.SetHideHp(dto.Identity.NpcId);
        }

        int x = 0;
        int y = 0;
        int z = 0;
        if (MaskBit(mask, 0, 0x08))
        {
            x = reader.ReadI();
            y = reader.ReadI();
            z = reader.ReadI();
            dto.Identity.SetL2jPos(x, y, z);
        }

        if (MaskBit(mask, 0, 0x04))
        {
            float heading = reader.ReadI();
            dto.Identity.OrigHeading = VectorUtils.HeadingToUnityQuaternionForNpc(heading);
            dto.Identity.Heading = UnityEngine.Quaternion.Euler(0, dto.Identity.OrigHeading, 0);
        }

        if (MaskBit(mask, 0, 0x02)) reader.ReadI(); // unknown
        if (MaskBit(mask, 0, 0x01))
        {
            dto.Stats.MAtkSpd = reader.ReadI();
            dto.Stats.PAtkSpd = reader.ReadI();
        }

        if (MaskBit(mask, 1, 0x80))
        {
            float moveAnim = reader.ReadF();
            float atkSpeed = reader.ReadF();
            dto.Stats.WalkRealSpeed = moveAnim;
            dto.Stats.RunRealSpeed = moveAnim;
            dto.Stats.PAtkRealSpeed = atkSpeed;
        }

        if (MaskBit(mask, 1, 0x40))
        {
            int rhand = reader.ReadI();
            reader.ReadI(); // chest
            int lhand = reader.ReadI();
            dto.Appearance.RHand = rhand;
            dto.Appearance.LHand = lhand;
        }

        if (MaskBit(mask, 1, 0x20)) reader.ReadB(); // dead
        if (MaskBit(mask, 1, 0x10)) dto.Identity.IsRunning = reader.ReadB() == 1;
        if (MaskBit(mask, 1, 0x02)) reader.ReadB(); // flying
        if (MaskBit(mask, 1, 0x01)) reader.ReadB(); // ordinal
        if (MaskBit(mask, 2, 0x80)) reader.ReadI(); // enchant
        if (MaskBit(mask, 2, 0x40)) reader.ReadI(); // flying 2
        if (MaskBit(mask, 2, 0x20)) reader.ReadI(); // clone
        if (MaskBit(mask, 2, 0x10)) reader.ReadI(); // pet
        if (MaskBit(mask, 2, 0x02)) reader.ReadI(); // state
        if (MaskBit(mask, 2, 0x01)) reader.ReadI(); // transformation

        if (MaskBit(mask, 3, 0x80)) dto.Status.SetHp(reader.ReadI());
        if (MaskBit(mask, 3, 0x40)) dto.Status.SetMp(reader.ReadI());
        if (MaskBit(mask, 3, 0x20)) dto.Stats.MaxHp = reader.ReadI();
        if (MaskBit(mask, 3, 0x10)) dto.Stats.MaxMp = reader.ReadI();
        if (MaskBit(mask, 3, 0x08)) dto.Identity.SummonState = reader.ReadB();
        if (MaskBit(mask, 3, 0x04))
        {
            reader.ReadI();
            reader.ReadI();
        }

        if (MaskBit(mask, 0, 0x10)) dto.Identity.Name = reader.ReadOtherS();
        if (MaskBit(mask, 3, 0x01)) reader.ReadI();
        if (MaskBit(mask, 4, 0x80)) reader.ReadI();
        if (MaskBit(mask, 4, 0x40)) reader.ReadB();
        if (MaskBit(mask, 4, 0x20)) reader.ReadI();
        if (MaskBit(mask, 4, 0x10))
        {
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
            reader.ReadI();
        }

        if (MaskBit(mask, 4, 0x04)) reader.ReadB();
        if (MaskBit(mask, 4, 0x08))
        {
            int length = reader.ReadSh();
            for (int i = 0; i < length; i++) reader.ReadSh();
        }

        return dto;
    }

    private static bool MaskBit(byte[] mask, int index, int bit)
    {
        return index < mask.Length && (mask[index] & bit) != 0;
    }

    private static CharSelectedDto ParseCharSelected(PacketReader reader)
    {
        PlayerInfoInterlude info = new PlayerInfoInterlude
        {
            Identity = new EntityIdentity(),
            Status = new PlayerStatus(),
            Stats = new PlayerStats(),
            Appearance = new PlayerAppearance(),
        };

        info.Identity.Owned = true;
        info.Identity.Name = reader.ReadOtherS();
        info.Identity.Id = reader.ReadI();
        info.Identity.Title = reader.ReadOtherS();
        reader.ReadI(); // session id
        info.Identity.ClanId = reader.ReadI();
        reader.ReadI(); // unk
        int sex = reader.ReadI();
        int race = reader.ReadI();
        int classId = reader.ReadI();
        reader.ReadI(); // active
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        info.Identity.PlayerClass = classId;
        info.Identity.SetL2jPos(x, y, z);
        info.Appearance.Sex = sex;
        info.Appearance.Race = race;
        info.Appearance.BaseClass = classId;

        info.Status.SetHp(reader.ReadD());
        info.Status.SetMp(reader.ReadD());
        long sp = reader.ReadLOther();
        long exp = reader.ReadLOther();
        int level = reader.ReadI();
        int karma = reader.ReadI();
        int pkKills = reader.ReadI();
        info.Stats.Level = level;
        info.Stats.Exp = exp;
        info.Stats.Sp = sp;
        info.Stats.Karma = karma;
        info.Stats.PkKills = pkKills;
        info.Stats.MaxExp = LevelServer.GetExp(level + 1);
        info.Stats.Speed = 50;
        info.Stats.AttackRange = 5;
        info.Stats.PCritical = 5;
        info.Stats.MCritical = 5;

        reader.ReadI(); // game time
        reader.ReadI(); // unk
        reader.ReadI(); // class id 2
        reader.ReadB(16);
        for (int i = 0; i < 9; i++) reader.ReadI();
        reader.ReadB(28);
        reader.ReadI();

        return CharSelectedDto.From(info);
    }
}
