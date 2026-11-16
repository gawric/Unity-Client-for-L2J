using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class LocalDemoMerchantProbe : MonoBehaviour
{
    private NpcEntity _inspectionEntity;
    public string Mode;
    public int NpcId;
    public int ExpectedPotions = 2;

    [Serializable]
    public sealed class Result
    {
        public string mode;
        public int npcId;
        public int npcObjectId;
        public bool npcRendered;
        public bool npcAnimated;
        public bool npcMaterialsValid;
        public string npcClip;
        public bool inspectionCamera;
        public Vector3 npcPosition;
        public float feetAboveRoot;
        public string groundHits;
        public int itemId;
        public int quantity;
        public int unitPrice;
        public int beforeAdena;
        public int afterAdena;
        public int beforeCount;
        public int afterCount;
        public long displayedPrice;
        public int necklaceCount;
        public bool emptyBasketBlocked;
        public bool partialReturn;
        public bool canceledQuantityCleared;
        public bool invalidQuantityBlocked;
        public bool insufficientFundsBlocked;
        public bool duplicateWeaponBlocked;
        public bool equipped;
        public bool success;
        public string error;
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(10);
        var result = new Result { mode = Mode };
        PlayerInventory inventory = PlayerInventory.Instance;
        DealerWindow dealer = DealerWindow.Instance;
        if (inventory == null || dealer == null) { Save(result, "Inventory or dealer unavailable"); yield break; }
        if (Mode == "deposit" || Mode == "withdraw")
        {
            yield return Warehouse(result, dealer);
            yield break;
        }
        bool buying = Mode != "sell";
        bool weapon = Mode == "weapon";
        result.itemId = weapon ? 1 : 1060;
        result.quantity = weapon ? 1 : buying ? 5 : 3;
        result.beforeAdena = Count(57);
        result.beforeCount = Count(result.itemId);
        if (Mode == "verify")
        {
            result.afterAdena = result.beforeAdena;
            result.afterCount = result.beforeCount;
            result.equipped = Equipped(1);
            result.success = result.equipped && Count(1060) == ExpectedPotions;
            InventoryWindow.Instance.ShowWindow();
            Save(result, result.success ? null : "Persisted potion or sword state differs");
            yield break;
        }
        result.npcId = NpcId > 0 ? NpcId : weapon ? 30001 : 30004;
        NpcInfoDto npc = StorageNpc.getInstance().CopyNpcs().FirstOrDefault(item => item.Identity.NpcId == result.npcId);
        if (npc == null) { Save(result, "Expected merchant not received from server"); yield break; }
        result.npcObjectId = npc.Identity.Id;
        result.npcRendered = World.Instance.ContainsNpc(result.npcObjectId);
        NpcEntity entity = World.Instance.NpcsContainer.GetComponentsInChildren<NpcEntity>().FirstOrDefault(item => item.Identity.Id == result.npcObjectId);
        if (entity == null) { Save(result, "Merchant has no rendered entity"); yield break; }
        var skins = entity.GetComponentsInChildren<SkinnedMeshRenderer>();
        result.npcMaterialsValid = skins.Length > 0 && skins.All(skin => skin.sharedMaterials.All(material => material != null && material.shader.isSupported && material.mainTexture != null));
        Animator npcAnimator = entity.GetComponent<Animator>();
        if (npcAnimator != null)
            result.npcClip = string.Join(",", npcAnimator.GetCurrentAnimatorClipInfo(0).Select(item => item.clip.name));
        if (NpcId == 30150)
        {
            Mesh first = new Mesh();
            Mesh second = new Mesh();
            skins[0].BakeMesh(first);
            yield return new WaitForSecondsRealtime(0.7f);
            skins[0].BakeMesh(second);
            Vector3[] a = first.vertices;
            Vector3[] b = second.vertices;
            result.npcAnimated = a.Length == b.Length && a.Where((point, index) => Vector3.Distance(point, b[index]) > 0.00001f).Any();
            UnityEngine.Object.Destroy(first);
            UnityEngine.Object.Destroy(second);
            if (!result.npcMaterialsValid || !result.npcAnimated) { Save(result, "Imported merchant materials or live animation failed"); yield break; }
        }
        if (Mode == "inspect")
        {
            HtmlWindow.Instance.HideWindow();
            _inspectionEntity = entity;
            result.inspectionCamera = true;
            result.npcPosition = VectorUtils.ConvertPosUnityToL2j(entity.transform.position);
            Mesh baked = new Mesh();
            skins[0].BakeMesh(baked);
            result.feetAboveRoot = baked.vertices.Min(point => skins[0].transform.TransformPoint(point).y) - entity.transform.position.y;
            UnityEngine.Object.Destroy(baked);
            result.groundHits = string.Join(";", Physics.RaycastAll(entity.transform.position + Vector3.up * 2, Vector3.down, 5, ~0, QueryTriggerInteraction.Ignore)
                .Where(hit => hit.collider.GetComponentInParent<Entity>() == null)
                .OrderByDescending(hit => hit.point.y)
                .Select(hit => hit.collider.name + "@" + VectorUtils.ConvertPosUnityToL2j(hit.point).z.ToString("F1")));

            result.success = result.npcRendered && result.npcMaterialsValid && result.npcAnimated;
            Save(result, result.success ? null : "NPC inspection failed");
            yield break;
        }
        Vector3 position = VectorUtils.ConvertPosUnityToL2j(PlayerEntity.Instance.transform.position);
        GameClient.Instance.Send(new ClickActionCommand(result.npcObjectId, (int)position.x, (int)position.y, (int)position.z, 0));
        yield return new WaitForSecondsRealtime(1);
        GameClient.Instance.Send(new ClickActionCommand(result.npcObjectId, (int)position.x, (int)position.y, (int)position.z, 0));
        yield return new WaitForSecondsRealtime(2);
        int list = result.npcId * 100;
        GameClient.Instance.Send(new RequestBypassToServerCommand("npc_" + result.npcObjectId + (buying ? "_Buy " + list : "_Sell")));
        float deadline = Time.realtimeSinceStartup + 15;
        while (dealer.CurrentProductType != (buying ? ProductType.BUY : ProductType.SELL) || dealer.CurrentListId != (buying ? list : 0))
        {
            if (Time.realtimeSinceStartup > deadline) { Save(result, "Merchant list timeout"); yield break; }
            yield return new WaitForSecondsRealtime(0.25f);
        }
        result.emptyBasketBlocked = !dealer.ConfirmSelection();
        Product product = dealer.GetListBuy().FirstOrDefault(item => item.ItemId == result.itemId);
        if (product == null) { Save(result, "Expected item absent from merchant list"); yield break; }
        result.unitPrice = product.Price;
        int sourceIndex = dealer.GetListBuy().IndexOf(product);
        var sourceSlot = dealer.GetInventorySlotsSell()[sourceIndex].SlotElement;
        dealer.EventDoubleClick(sourceSlot);
        if (!weapon)
        {
            QuantityInput.Instance.CancelSelection();
            dealer.EventDoubleClick(sourceSlot);
            result.invalidQuantityBlocked = !QuantityInput.Instance.SubmitQuantity("0") &&
                !QuantityInput.Instance.SubmitQuantity("2147483648");
            if (!QuantityInput.Instance.SubmitQuantity(result.quantity.ToString())) { Save(result, "Quantity submission failed"); yield break; }
            result.canceledQuantityCleared = dealer.GetListSell().Count == 1 && dealer.GetListSell()[0].Count == result.quantity;
            dealer.EventDoubleClick(dealer.GetInventorySlotsBuy()[0].SlotElement);
            QuantityInput.Instance.SubmitQuantity("1");
            result.partialReturn = dealer.GetListSell().Count == 1 && dealer.GetListSell()[0].Count == result.quantity - 1;
            int available = dealer.GetListBuy().FindIndex(item => item.ItemId == result.itemId);
            dealer.EventDoubleClick(dealer.GetInventorySlotsSell()[available].SlotElement);
            if (!QuantityInput.Instance.SubmitQuantity("1") || !result.partialReturn || !result.canceledQuantityCleared)
            { Save(result, "Partial return or canceled quantity cleanup failed"); yield break; }
        }
        else
        {
            dealer.EventDoubleClick(sourceSlot);
            result.duplicateWeaponBlocked = dealer.GetListSell().Count == 1 && dealer.GetListSell()[0].Count == 1;
        }
        if (!dealer.ConfirmSelection()) { Save(result, "Transaction confirmation blocked"); yield break; }
        deadline = Time.realtimeSinceStartup + 20;
        int expectedCount = result.beforeCount + (buying ? result.quantity : -result.quantity);
        long expectedMoney = result.beforeAdena + (buying ? -1L : 1L) * result.unitPrice * result.quantity;
        do
        {
            yield return new WaitForSecondsRealtime(0.25f);
            result.afterAdena = Count(57);
            result.afterCount = Count(result.itemId);
        }
        while ((result.afterAdena != expectedMoney || result.afterCount != expectedCount) && Time.realtimeSinceStartup < deadline);
        if (result.afterAdena != expectedMoney || result.afterCount != expectedCount) { Save(result, "Authoritative inventory or adena mismatch"); yield break; }
        if (weapon)
        {
            ItemInstance sword = inventory.GetItemByItemId(1);
            if (sword == null || !inventory.UseItem(sword.ObjectId)) { Save(result, "Cannot use purchased sword"); yield break; }
            deadline = Time.realtimeSinceStartup + 10;
            while (!Equipped(1) && Time.realtimeSinceStartup < deadline) yield return new WaitForSecondsRealtime(0.25f);
            result.equipped = Equipped(1);
            if (!result.equipped) { Save(result, "Equipment state timeout"); yield break; }
        }
        result.success = result.emptyBasketBlocked && (weapon ? result.duplicateWeaponBlocked : result.partialReturn && result.canceledQuantityCleared && result.invalidQuantityBlocked);
        InventoryWindow.Instance.ShowWindow();
        Save(result, result.success ? null : "Basket guard failed");
    }

    IEnumerator Warehouse(Result result, DealerWindow dealer)
    {
        bool deposit = Mode == "deposit";
        result.npcId = NpcId > 0 ? NpcId : 30005;
        result.itemId = 1060;
        result.quantity = 3;
        result.beforeAdena = Count(57);
        result.beforeCount = Count(result.itemId);
        var npc = StorageNpc.getInstance().CopyNpcs().FirstOrDefault(item => item.Identity.NpcId == result.npcId);
        if (npc == null) { Save(result, "Warehouse keeper was not received"); yield break; }
        result.npcObjectId = npc.Identity.Id;
        var entity = World.Instance.NpcsContainer.GetComponentsInChildren<NpcEntity>().FirstOrDefault(item => item.Identity.Id == npc.Identity.Id);
        if (entity == null) { Save(result, "Warehouse keeper model is absent"); yield break; }
        result.npcRendered = true;
        ClickManager.Instance.ClickEntity(entity.gameObject);
        yield return new WaitForSecondsRealtime(1);
        ClickManager.Instance.ClickEntity(entity.gameObject);
        yield return new WaitForSecondsRealtime(2);
        string command = "npc_" + npc.Identity.Id + (deposit ? "_DepositP" : "_WithdrawP");
        if (!HtmlWindow.Instance.TryActivateLink(command)) { Save(result, "Warehouse action was not offered"); yield break; }
        float deadline = Time.realtimeSinceStartup + 10;
        ProductType expectedType = deposit ? ProductType.WHDepositList : ProductType.WHWithdrawList;
        while (dealer.CurrentProductType != expectedType && Time.realtimeSinceStartup < deadline)
            yield return new WaitForSecondsRealtime(0.25f);
        if (dealer.CurrentProductType != expectedType) { Save(result, "Warehouse list timeout"); yield break; }
        result.emptyBasketBlocked = !dealer.ConfirmSelection();
        int index = dealer.GetListBuy().FindIndex(item => item.ItemId == result.itemId);
        if (index < 0) { Save(result, "Warehouse source stack is absent"); yield break; }
        dealer.EventDoubleClick(dealer.GetInventorySlotsSell()[index].SlotElement);
        QuantityInput.Instance.CancelSelection();
        dealer.EventDoubleClick(dealer.GetInventorySlotsSell()[index].SlotElement);
        result.invalidQuantityBlocked = !QuantityInput.Instance.SubmitQuantity("0") && !QuantityInput.Instance.SubmitQuantity("2147483648");
        if (!QuantityInput.Instance.SubmitQuantity("3")) { Save(result, "Warehouse quantity submission failed"); yield break; }
        result.canceledQuantityCleared = dealer.GetListSell() != null && dealer.GetListSell().Count == 1 && dealer.GetListSell()[0].Count == 3;
        if (!result.canceledQuantityCleared) { Save(result, "Warehouse quantity cancel left duplicate callbacks or selection"); yield break; }
        dealer.EventDoubleClick(dealer.GetInventorySlotsBuy()[0].SlotElement);
        QuantityInput.Instance.SubmitQuantity("1");
        result.partialReturn = dealer.GetListSell().Count == 1 && dealer.GetListSell()[0].Count == 2;
        index = dealer.GetListBuy().FindIndex(item => item.ItemId == result.itemId);
        if (index < 0) { Save(result, "Returning one unit did not restore the source stack"); yield break; }
        dealer.EventDoubleClick(dealer.GetInventorySlotsSell()[index].SlotElement);
        QuantityInput.Instance.SubmitQuantity("1");
        if (!result.emptyBasketBlocked || !result.invalidQuantityBlocked || !result.partialReturn || dealer.GetListSell().Count != 1 || dealer.GetListSell()[0].Count != 3)
        { Save(result, "Warehouse basket guards failed"); yield break; }
        result.displayedPrice = dealer.DisplayedPrice;
        if (result.displayedPrice != (deposit ? 30 : 0)) { Save(result, "Warehouse fee display differs from the server charge"); yield break; }
        if (deposit)
        {
            dealer.UpdateAdena(29);
            result.insufficientFundsBlocked = !dealer.ConfirmSelection();
            dealer.UpdateAdena(result.beforeAdena);
            if (!result.insufficientFundsBlocked) { Save(result, "Warehouse accepted a basket above the displayed balance"); yield break; }
        }
        if (!dealer.ConfirmSelection()) { Save(result, "Warehouse confirmation blocked"); yield break; }
        int expectedCount = result.beforeCount + (deposit ? -3 : 3);
        int expectedAdena = result.beforeAdena - (deposit ? 30 : 0);
        deadline = Time.realtimeSinceStartup + 15;
        do
        {
            yield return new WaitForSecondsRealtime(0.25f);
            result.afterCount = Count(result.itemId);
            result.afterAdena = Count(57);
        }
        while ((result.afterCount != expectedCount || result.afterAdena != expectedAdena) && Time.realtimeSinceStartup < deadline);
        result.success = result.afterCount == expectedCount && result.afterAdena == expectedAdena;
        InventoryWindow.Instance.ShowWindow();
        Save(result, result.success ? null : "Warehouse item count or fee does not match the server result");
    }

    void LateUpdate()
    {
        if (_inspectionEntity == null || Camera.main == null) return;
        var skin = _inspectionEntity.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skin == null) return;
        Vector3 target = skin.bounds.center;
        Vector3 position = target + _inspectionEntity.transform.forward * Mathf.Max(1.2f, skin.bounds.size.y * 1.8f) + Vector3.up * 0.15f;
        Camera.main.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
    }

    static int Count(int itemId)
    {
        var inventory = PlayerInventory.Instance;
        return inventory.GetPlayerInventory().Values.Concat(inventory.GetPlayerEquipInventory().Values)
            .Where(item => item.ItemId == itemId).Sum(item => item.Count);
    }

    static bool Equipped(int itemId) => PlayerInventory.Instance.GetPlayerEquipInventory().Values.Any(item => item.ItemId == itemId);

    static void Save(Result result, string error)
    {
        result.necklaceCount = Count(906);
        result.error = error;
        string json = JsonUtility.ToJson(result);
        File.WriteAllText(Path.Combine(UnityAutoLogin.ResultDirectory, "autologin.merchant"), json);
        Debug.Log("[LocalDemoMerchant] " + json);
    }
}
