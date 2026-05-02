using System;

public static class PlayerCommands
{
    private const int MAX_ID = 113;
    private static readonly PlayerCommandData[] _byId;

    static PlayerCommands()
    {
        _byId = new PlayerCommandData[MAX_ID + 1];

        Register(PlayerCommand.LOC,                     0,  "/loc");
        Register(PlayerCommand.GM,                      1,  "/GM");
        Register(PlayerCommand.GMCANCEL,                2,  "/Gmcancel");
        Register(PlayerCommand.GMLIST,                  3,  "/gmlist");
        Register(PlayerCommand.SIT,                     4,  "/sit");
        Register(PlayerCommand.STAND,                   5,  "/stand");
        Register(PlayerCommand.WALK,                    6,  "/walk");
        Register(PlayerCommand.RUN,                     7,  "/run");
        Register(PlayerCommand.ATTACK,                  8,  "/attack");
        Register(PlayerCommand.ATTACKFORCE,             9,  "/attackforce");
        Register(PlayerCommand.ATTACKSTAND,             10, "/attackstand");
        Register(PlayerCommand.TRADE,                   11, "/trade");
        Register(PlayerCommand.TARGET,                  12, "/target");
        Register(PlayerCommand.TARGETNEXT,              13, "/targetnext");
        Register(PlayerCommand.PICKUP,                  14, "/pickup");
        Register(PlayerCommand.ASSIST,                  15, "/assist");
        Register(PlayerCommand.INVITE,                  16, "/Invite");
        Register(PlayerCommand.LEAVE,                   17, "/Leave");
        Register(PlayerCommand.DISMISS,                 18, "/Dismiss");
        Register(PlayerCommand.VENDOR,                  19, "/vendor");
        Register(PlayerCommand.PARTYMATCHING,           20, "/partymatching");
        Register(PlayerCommand.SOCIALHELLO,             21, "/socialhello");
        Register(PlayerCommand.SOCIALVICTORY,           22, "/socialvictory");
        Register(PlayerCommand.SOCIALCHARGE,            23, "/socialcharge");
        Register(PlayerCommand.USESKILL,                24, "/useskill");
        Register(PlayerCommand.USESKILLFORCE,           25, "/useskillforce");
        Register(PlayerCommand.USESKILLSTAND,           26, "/useskillstand");
        Register(PlayerCommand.USESHORTCUT,             27, "/useshortcut");
        Register(PlayerCommand.USESHORTCUTFORCE,        28, "/useshortcutforce");
        Register(PlayerCommand.USESHORTCUTSTAND,        29, "/useshortcutstand");
        Register(PlayerCommand.ALLYINVITE,              30, "/allyinvite");
        Register(PlayerCommand.ALLYLEAVE,               31, "/allyleave");
        Register(PlayerCommand.ALLYDISMISS,             32, "/allydismiss");
        Register(PlayerCommand.ALLYDISSOLVE,            33, "/allydissolve");
        Register(PlayerCommand.ALLYCREST,               34, "/allycrest");
        Register(PlayerCommand.ALLYINFO,                35, "/allyinfo");
        Register(PlayerCommand.FRIENDINVITE,            36, "/friendinvite");
        Register(PlayerCommand.FRIENDLIST,              37, "/friendlist");
        Register(PlayerCommand.FRIENDDEL,               38, "/frienddel");
        Register(PlayerCommand.BUY,                     39, "/buy");
        Register(PlayerCommand.ALLYWARSTART,            40, "/allywarstart");
        Register(PlayerCommand.ALLYWARSTOP,             41, "/allywarstop");
        Register(PlayerCommand.ALLYWARSURRENDER,        42, "/allywarsurrender");
        Register(PlayerCommand.BLOCKLIST,               43, "/blocklist");
        Register(PlayerCommand.BLOCK,                   44, "/block");
        Register(PlayerCommand.UNBLOCK,                 45, "/unblock");
        Register(PlayerCommand.SOCIALNO,                46, "/socialno");
        Register(PlayerCommand.SOCIALYES,               47, "/socialyes");
        Register(PlayerCommand.SOCIALBOW,               48, "/socialbow");
        Register(PlayerCommand.SOCIALUNAWARE,           49, "/socialunaware");
        Register(PlayerCommand.SOCIALWAITING,           50, "/socialwaiting");
        Register(PlayerCommand.SOCIALLAUGH,             51, "/sociallaugh");
        Register(PlayerCommand.UNSTUCK,                 52, "/unstuck");
        Register(PlayerCommand.REMAINTIME,              53, "/remaintime");
        Register(PlayerCommand.SOCIALAPPLAUSE,          54, "/socialapplause");
        Register(PlayerCommand.SOCIALDANCE,             55, "/socialdance");
        Register(PlayerCommand.SOCIALSAD,               56, "/socialsad");
        Register(PlayerCommand.SITSTAND,                57, "/sitstand");
        Register(PlayerCommand.WALKRUN,                 58, "/walkrun");
        Register(PlayerCommand.DWARVENMANUFACTURE,      59, "/dwarvenmanufacture");
        Register(PlayerCommand.MOUNTDISMOUNT,           60, "/mountdismount");
        Register(PlayerCommand.MOUNT,                   61, "/mount");
        Register(PlayerCommand.DISMOUNT,                62, "/dismount");
        Register(PlayerCommand.PETHOLD,                 63, "/pethold");
        Register(PlayerCommand.PETATTACK,               64, "/petattack");
        Register(PlayerCommand.PETSTOP,                 65, "/petstop");
        Register(PlayerCommand.PETCOLLECT,              66, "/petcollect");
        Register(PlayerCommand.PETREVERT,               67, "/petrevert");
        Register(PlayerCommand.PETSPECIALSKILL,         68, "/petspecialskill");
        Register(PlayerCommand.SUMMONHOLD,              69, "/summonhold");
        Register(PlayerCommand.SUMMONATTACK,            70, "/summonattack");
        Register(PlayerCommand.SUMMONSTOP,              71, "/summonstop");
        Register(PlayerCommand.SUMMONSIEGE,             72, "/summonsiege");
        Register(PlayerCommand.SUMMONPOISON,            73, "/summonpoison");
        Register(PlayerCommand.SUMMONCORPSE,            74, "/summoncorpse");
        Register(PlayerCommand.EVALUATE,                75, "/evaluate");
        Register(PlayerCommand.DELAY,                   76, "/delay");
        Register(PlayerCommand.TIME,                    77, "/Time");
        Register(PlayerCommand.ALLBLOCK,                78, "/allblock");
        Register(PlayerCommand.ALLUNBLOCK,              79, "/allunblock");
        Register(PlayerCommand.EQUIP,                   80, "/equip");
        Register(PlayerCommand.PARTYINFO,               81, "/partyinfo");
        Register(PlayerCommand.CLANWARINFO,             82, "/clanwarinfo");
        Register(PlayerCommand.ALLYWARINFO,             83, "/allywarinfo");
        Register(PlayerCommand.FINDPRIVATESTORE,        84, "/findprivatestore");
        Register(PlayerCommand.CHANGEPARTYLEADER,       85, "/changepartyleader");
        Register(PlayerCommand.CLANWARSTART,            86, "/clanwarstart");
        Register(PlayerCommand.CLANWARSTOP,             87, "/clanwarstop");
        Register(PlayerCommand.ATTACKLIST,              88, "/attacklist");
        Register(PlayerCommand.UNDERATTACKLIST,         89, "/underattacklist");
        Register(PlayerCommand.WARLIST,                 90, "/warlist");
        Register(PlayerCommand.DELETEALLIANCECREST,     91, "/deletealliancecrest");
        Register(PlayerCommand.CHANNELCREATE,           92, "/channelcreate");
        Register(PlayerCommand.CHANNELDELETE,           93, "/channeldelete");
        Register(PlayerCommand.CHANNELINVITE,           94, "/channelinvite");
        Register(PlayerCommand.CHANNELKICK,             95, "/channelkick");
        Register(PlayerCommand.CHANNELLEAVE,            96, "/channelleave");
        Register(PlayerCommand.CHANNELINFO,             97, "/channelinfo");
        Register(PlayerCommand.NICK,                    98, "/nick");
        Register(PlayerCommand.SIEGESTATUS,             99, "/siegestatus");
        Register(PlayerCommand.CLANPENALTY,             100,"/clanpenalty");
        Register(PlayerCommand.GENERALMANUFACTURE,      101,"/generalmanufacture");
        Register(PlayerCommand.FATIGUETIME,             102,"/fatiguetime");
        Register(PlayerCommand.START_VIDEORECORDING,    103,"/start_videorecording");
        Register(PlayerCommand.END_VIDEORECORDING,      104,"/end_videorecording");
        Register(PlayerCommand.STARTEND_VIDEORECORDING, 105,"/startend_videorecording");
        Register(PlayerCommand.PETMOVE,                 106,"/petmove");
        Register(PlayerCommand.SERVITORMOVE,            107,"/servitormove");
        Register(PlayerCommand.UNSUMMON,                108,"/unsummon");
        Register(PlayerCommand.OLYMPIADSTAT,            109,"/olympiadstat");
        Register(PlayerCommand.GRADUATELIST,            110,"/graduatelist");
        Register(PlayerCommand.DUEL,                    111,"/duel");
        Register(PlayerCommand.WITHDRAW,                112,"/withdraw");
        Register(PlayerCommand.PARTYDUEL,               113,"/partyduel");
    }

    private static void Register(PlayerCommand type, int id, string command)
    {
        _byId[id] = new PlayerCommandData(id, type, command);
    }

    public static PlayerCommandData GetById(int id)
    {
        if ((uint)id <= MAX_ID)
        {
            var data = _byId[id];
            return data != null && data.Id == id ? data : null;
        }

        return null;
    }

    public static PlayerCommandData FindByText(string text)
    {
        if (string.IsNullOrEmpty(text) || text[0] != '/')
            return null;

        for (int i = 0; i <= MAX_ID; i++)
        {
            var cmd = _byId[i];
            if (cmd != null && text.StartsWith(cmd.Command, StringComparison.Ordinal))
                return cmd;
        }

        return null;
    }
}