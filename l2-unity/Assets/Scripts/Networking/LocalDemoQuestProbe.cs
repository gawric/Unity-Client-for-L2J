using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed class LocalDemoQuestProbe : MonoBehaviour
{
    public int NpcId;
    public int ExpectedItem;
    public bool StartQuest;
    public bool ExpectUnavailable;
    const string QuestName = "Q00001_LettersOfLove";
    private string _error;
    private Result _result;

    [Serializable]
    public sealed class Result
    {
        public int npcId;
        public int expectedItem;
        public int beforeCount;
        public int afterCount;
        public bool npcRendered;
        public bool unavailable;
        public bool success;
        public string error;
        public List<string> activatedLinks = new List<string>();
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(10);
        _result = new Result { npcId = NpcId, expectedItem = ExpectedItem, beforeCount = Count(ExpectedItem) };
        if (ExpectedItem > 0 && _result.beforeCount > 0) { Finish("Step item already owned; do not repeat quest action"); yield break; }
        var npc = StorageNpc.getInstance().CopyNpcs().FirstOrDefault(item => item.Identity.NpcId == NpcId);
        if (npc == null) { Finish("Quest NPC not received"); yield break; }
        var entity = World.Instance.NpcsContainer.GetComponentsInChildren<NpcEntity>().FirstOrDefault(item => item.Identity.Id == npc.Identity.Id);
        if (entity == null) { Finish("Quest NPC model is absent"); yield break; }
        _result.npcRendered = true;
        string previous = HtmlWindow.Instance.CurrentHtml;
        ClickManager.Instance.ClickEntity(entity.gameObject);
        yield return new WaitForSecondsRealtime(1);
        ClickManager.Instance.ClickEntity(entity.gameObject);
        yield return WaitReply(previous);
        if (_error != null) { Finish(_error); yield break; }
        yield return Activate("Script");
        if (_error != null) { Finish(_error); yield break; }
        string choose = "npc_" + npc.Identity.Id + "_Script " + QuestName;
        previous = HtmlWindow.Instance.CurrentHtml;
        if (HtmlWindow.Instance.TryActivateLink(choose))
        {
            _result.activatedLinks.Add(choose);
            yield return WaitReply(previous);
            if (_error != null) { Finish(_error); yield break; }
        }
        if (ExpectUnavailable)
        {
            _result.unavailable = HtmlWindow.Instance.CurrentHtml.IndexOf("level 2 and above", StringComparison.OrdinalIgnoreCase) >= 0;
            _result.success = _result.unavailable && Count(687) == 0;
            Finish(_result.success ? null : "Expected level gate was not shown");
            yield break;
        }
        if (StartQuest)
        {
            foreach (string page in new[] { "30048-03.htm", "30048-04.htm", "30048-06.htm" })
            {
                yield return Activate("Script " + QuestName + " " + page);
                if (_error != null) { Finish(_error); yield break; }
            }
        }
        float deadline = Time.realtimeSinceStartup + 15;
        do
        {
            yield return new WaitForSecondsRealtime(0.25f);
            _result.afterCount = Count(ExpectedItem);
        }
        while (_result.afterCount != _result.beforeCount + 1 && Time.realtimeSinceStartup < deadline);
        _result.success = _result.afterCount == _result.beforeCount + 1;
        Finish(_result.success ? null : "Expected quest item update was not received");
    }

    IEnumerator Activate(string command)
    {
        string previous = HtmlWindow.Instance.CurrentHtml;
        if (!HtmlWindow.Instance.TryActivateLink(command))
        {
            _error = "Link was not offered by the server: " + command;
            yield break;
        }
        _result.activatedLinks.Add(command);
        yield return WaitReply(previous);
    }

    IEnumerator WaitReply(string previous)
    {
        float deadline = Time.realtimeSinceStartup + 8;
        while (HtmlWindow.Instance.CurrentHtml == previous && Time.realtimeSinceStartup < deadline)
            yield return new WaitForSecondsRealtime(0.25f);
        if (HtmlWindow.Instance.CurrentHtml == previous) _error = "NPC HTML reply timeout";
        // The server throttles successive bypasses even when the HTML response is immediate.
        // Pace this automation like a player; keep the normal server flood protection intact.
        if (_error == null) yield return new WaitForSecondsRealtime(0.75f);
    }

    static int Count(int itemId)
    {
        if (itemId <= 0 || PlayerInventory.Instance == null) return 0;
        return PlayerInventory.Instance.GetPlayerInventory().Values.Where(item => item.ItemId == itemId).Sum(item => item.Count);
    }

    void Finish(string error)
    {
        _result.error = error;
        string json = JsonUtility.ToJson(_result);
        File.WriteAllText(Path.Combine(UnityAutoLogin.ResultDirectory, "autologin.quest"), json);
        File.WriteAllText(Path.Combine(UnityAutoLogin.ResultDirectory, "autologin.quest-html"), HtmlWindow.Instance.CurrentHtml);
        Debug.Log("[LocalDemoQuest] " + json);
    }
}
