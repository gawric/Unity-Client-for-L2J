using UnityEngine;
using UnityEngine.UIElements;

public class DataProviderDetailedInfo : AbstractDataFunction
{
   public void SetDataInfo(VisualElement container, QuestInstance quest)
   {
        if(container != null)
        {
            Label titleLabel = container.Q<Label>("titleLabel");
            AddElementIfNotEmpty(titleLabel , titleLabel , quest.QuestName());

            Label lvlLabel = container.Q<Label>("lvlLabel");
            VisualElement lvlGroup = container.Q<VisualElement>("GroupLevelRange");
            Debug.Log("LevelRange " + quest.LevelRange());
            AddElementIfNotEmpty(lvlGroup, lvlLabel, quest.LevelRange());


            Label timeLabel = container.Q<Label>("timeName");
            VisualElement timeGroup = container.Q<VisualElement>("GroupTimeName");
            AddElementIfNotEmpty(timeGroup, timeLabel, quest.IsRepeat() ? "Repeatable" : "One time");

            Label textlabel = container.Q<Label>("textlabel");
            AddElementIfNotEmpty(textlabel, textlabel, quest.GetDescription());

        }
   }
}
