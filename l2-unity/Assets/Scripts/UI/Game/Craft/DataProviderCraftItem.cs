using UnityEngine;
using UnityEngine.UIElements;

public class DataProviderCraftItem : AbstractDataFunction
{


    public void SetDataInfo(VisualElement container, ItemInstance itemInstance , RecipeData recipeData)
    {
        if (container != null)
        {

            //labelName
            //labelMpData
            //labelChanceData
            //labelReceiveData
            //labellQuantityData

            int rate = recipeData.SuccessRate;
            int mpCost = recipeData.MpCost;


            IDataTips text = ToolTipManager.GetInstance().GetProductText(itemInstance);

            VisualElement groupBoxIcon = container.Q<VisualElement>("GrowIcon");
            VisualElement icon = container.Q<VisualElement>("icon");
            Texture2D texture = IconManager.Instance.GetIcon(itemInstance.ItemId);
            AddElementIfNotNull(groupBoxIcon, icon, texture);

            VisualElement gradeBox = container.Q<VisualElement>("grade");
            Texture2D grade = text.GetGradeTexture();
            SetImageElement(gradeBox, grade);

            string name = text.GetName(true);
            Label labelName = container.Q<Label>("labelName");
            AddElementIfNotEmpty(labelName, labelName, name);

            string mp = mpCost.ToString();
            Label labelMpData = container.Q<Label>("labelMpData");
            AddElementIfNotEmpty(labelMpData, labelMpData, mp);

            string chance = rate + "%";
            Label labelChanceData = container.Q<Label>("labelChanceData");
            AddElementIfNotEmpty(labelChanceData, labelChanceData, chance);

            string count = itemInstance.Count.ToString();
            Label labelReceiveData = container.Q<Label>("labelReceiveData");
            AddElementIfNotEmpty(labelReceiveData, labelReceiveData, count);

            ItemInstance inventory = PlayerInventory.Instance.GetItemByItemId(itemInstance.ItemId);
            string quantity = (inventory == null)? "0" : inventory.Count.ToString();
            Label labellQuantityData = container.Q<Label>("labellQuantityData");
            AddElementIfNotEmpty(labellQuantityData, labellQuantityData, quantity);

        }
    }
}
