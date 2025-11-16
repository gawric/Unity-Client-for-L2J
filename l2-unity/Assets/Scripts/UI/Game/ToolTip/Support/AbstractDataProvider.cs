using UnityEngine;
using UnityEngine.UIElements;

public abstract class AbstractDataProvider : AbstractDataFunction
{
 
    public VisualElement SetDataSkillInTemplate(VisualElement container, SkillInstance skill, IDataTips text , float timeleft)
    {
        VisualElement returnElement = null;

        container.Q<Label>("nameSkill").text = text.GetName();


        VisualElement centerBox = container.Q<VisualElement>("CenterBox");
        VisualElement footerBox = container.Q<VisualElement>("FooterBox");

        VisualElement groupTypeLvl = container.Q<VisualElement>("lvlSkill");
        Label lvlSkill = container.Q<Label>("lvlSkill");

        VisualElement groupTypeLabelLvl = container.Q<VisualElement>("LvlLabel");
        Label lvlLabel = container.Q<Label>("LvlLabel");

        if(groupTypeLvl != null) AddElementIfNotEmpty(groupTypeLabelLvl, lvlLabel, "Lv.");
        AddElementIfNotEmpty(groupTypeLvl, lvlSkill, skill.Level.ToString());

        VisualElement groupTypeActiveSkill= container.Q<VisualElement>("SettingType");
        Label typeLabel = container.Q<Label>("typeSkill");
        AddElementIfNotEmpty(groupTypeActiveSkill, typeLabel, skill.GetTypeName());

        VisualElement groupTypeHp = container.Q<VisualElement>("HpText");
        Label hpLabel = container.Q<Label>("hpLabel");
        AddElementIfNot0(groupTypeHp, hpLabel, skill.GetHp());

        VisualElement groupTypeMp = container.Q<VisualElement>("MPText");
        Label mpLabel = container.Q<Label>("mpLabel");
        AddElementIfNot0(groupTypeMp, mpLabel, skill.GetMp());

        VisualElement groupTypeRadius = container.Q<VisualElement>("RadiusText");
        Label radiusLabel = container.Q<Label>("rangeLabel");
        AddElementIfNot0(groupTypeRadius, radiusLabel, skill.GetRange());

        VisualElement groupTypeTimeLeft = container.Q<VisualElement>("TimeLeftText");
        Label timeLeftLabel = container.Q<Label>("timeLeftLabel");


        if(timeleft != 0)
        {
            AddElementIfNotEmpty(groupTypeTimeLeft, timeLeftLabel, TimeUtils.FormatTime(timeleft));
            returnElement =  timeLeftLabel;
        }



        if (skill.GetHp() == 0 && skill.GetMp() == 0 && skill.GetRange() == -1 && timeleft == 0)
        {
            AddBoxIfNotEmpty(centerBox, "");
        }

        if (string.IsNullOrEmpty(text.GetDiscription()))
        {
            AddBoxIfNotEmpty(footerBox, text.GetDiscription());
        }

        VisualElement groupTypeCasting = container.Q<VisualElement>("CastingTimeText");
        Label castingLabel = container.Q<Label>("castlabel");


        VisualElement groupTypeReuse = container.Q<VisualElement>("ReuseTimeText");
        Label reuseLabel = container.Q<Label>("reuselabel");

        AddElementIfNot0(groupTypeReuse, reuseLabel, skill.GetReuseTime());

        AddElementIfNot0(groupTypeCasting, castingLabel, skill.GetHitTime());


        VisualElement groupTypeDesc = container.Q<VisualElement>("DescriptedText");
        Label descLabel = container.Q<Label>("descriptedLabel");

        AddElementIfNotEmpty(groupTypeDesc, descLabel, text.GetDiscription());

        VisualElement groupIcon = container.Q<VisualElement>("GrowIcon");
        var icon = container.Q<VisualElement>("icon");
        AddElementIfNotNull(groupIcon, icon, IconManager.Instance.LoadTextureByName(skill.Icon()));

        return returnElement;
    }

    protected void SetDataWeaponInTemplate(VisualElement container, Weapongrp weapon, int price , IDataTips text)
    {
        container.Q<Label>("nameWeapon").text = text.GetName();

        VisualElement gradeBox = container.Q<VisualElement>("grade");
        Texture2D grade = text.GetGradeTexture();
        SetImageElement(gradeBox, grade);

        VisualElement groupPriceLabel = container.Q<VisualElement>("PriceName");
        Label priceLabel = (Label)container.Q<Label>("priceLabel");
        AddElementPriceifNot0(groupPriceLabel, priceLabel, price);

        VisualElement groupType = container.Q<VisualElement>("SettingType");
        Label typeWeapon = container.Q<Label>("typeWeapon");
        AddElementIfNotEmpty(groupType, typeWeapon, WeaponTypeParser.WeaponTypeName(weapon.WeaponType));


        Label labelEnchant = container.Q<Label>("enchant");
        AddElementEnchantifNot0(labelEnchant, labelEnchant, text.GetEnchant());

        VisualElement groupPhys = container.Q<VisualElement>("phisAtkText");
        Label physLabel = container.Q<Label>("physLabel");
        AddElementIfNotEmpty(groupPhys, physLabel, weapon.PAtk.ToString());

        VisualElement groupMag = container.Q<VisualElement>("magAtkText");
        Label magLabel = container.Q<Label>("magLabel");
        AddElementIfNotEmpty(groupMag, magLabel, weapon.Matk.ToString());



        VisualElement groupSpd = container.Q<VisualElement>("speedText");
        Label spdLabel = container.Q<Label>("spdLabel");
        AddElementIfNotEmpty(groupSpd, spdLabel, weapon.GetSpeedName());



        VisualElement groupCrit = container.Q<VisualElement>("critText");
        Label critLabel = container.Q<Label>("critLabel");
        AddElementIfNotEmpty(groupCrit, critLabel, weapon.CriticalRate.ToString());



        VisualElement groupSoul = container.Q<VisualElement>("soulText");
        Label soulLabel = container.Q<Label>("soullabel");
        AddElementIfNotEmpty(groupSoul, soulLabel, "X" + weapon.Soulshot.ToString());



        VisualElement groupSpirit = container.Q<VisualElement>("spiritText");
        Label spiritLabel = container.Q<Label>("spiritlabel");
        AddElementIfNotEmpty(groupSpirit, spiritLabel, "X" + weapon.Spiritshot.ToString());


        VisualElement groupWeight = container.Q<VisualElement>("weightText");
        Label weightLabel = container.Q<Label>("weightlabel");
        AddElementIfNotEmpty(groupWeight, weightLabel, weapon.Weight.ToString());


        VisualElement groupDescr = container.Q<VisualElement>("descriptedText");
        Label descrLabel = container.Q<Label>("descriptedLabel");
        AddElementIfNotEmpty(groupDescr, descrLabel, text.GetItemDiscription());


        VisualElement groupIcon = container.Q<VisualElement>("GrowIcon");
        var icon = container.Q<VisualElement>("icon");
        AddElementIfNotNull(groupIcon, icon, IconManager.Instance.LoadTextureByName(weapon.Icon));
    }


    public void SetDataAccessoriesInTemplate(VisualElement container, Armorgrp armor, IDataTips text , int price , string accessoriesName)
    {
        container.Q<Label>("nameAccessories").text = text.GetName();

        VisualElement gradeBox = container.Q<VisualElement>("grade");
        Texture2D grade = text.GetGradeTexture();
        SetImageElement(gradeBox, grade);

        VisualElement groupBoxTypeS1 = container.Q<VisualElement>("typeTextS1");
        Label typeS1 = container.Q<Label>("typeLabelS1");
        VisualElement groupBoxMDef = container.Q<VisualElement>("mdeText");

        VisualElement groupBoxIcon = container.Q<VisualElement>("GrowIcon");
        VisualElement icon = container.Q<VisualElement>("icon");
        Texture2D texture = IconManager.Instance.LoadTextureByName(armor.Icon);
        AddElementIfNotNull(groupBoxIcon, icon, texture);
        AddElementIfNotEmpty(groupBoxTypeS1, typeS1, "");

        Label labelEnchant = container.Q<Label>("enchant");
        AddElementEnchantifNot0(labelEnchant, labelEnchant, text.GetEnchant());


        //EnabledRow(groupBoxMDef);

        Label mpLabel = container.Q<Label>("mpLabel");
        VisualElement mpGroup = container.Q<VisualElement>("mpText");
        AddElementIfNot0(mpGroup, mpLabel, armor.MpBonus);

        Label textMdef = container.Q<Label>("mdefLabel");
        //textMdef.text = armor.MDef.ToString();
        AddElementIfNotEmpty(groupBoxMDef, textMdef, armor.MDef.ToString());

        VisualElement groupTypeLabel = container.Q<VisualElement>("SettingType");
        Label typeLabel = container.Q<Label>("typeLabel");
        AddElementIfNotEmpty(groupTypeLabel, typeLabel, accessoriesName);

        Label groupPriceLabel = (Label)container.Q<Label>("PriceName");
        Label priceLabel = (Label)container.Q<Label>("priceLabel");
        AddElementPriceifNot0(groupPriceLabel, priceLabel, price);



        //typeLabel.text = product.GetTypeAccessoriesName();

        container.Q<Label>("weightlabel").text = armor.Weight.ToString();
        container.Q<Label>("descriptedLabel").text = text.GetItemDiscription();
    }


    protected void SetOther(VisualElement container , IDataTips text , string etcIcon , int priceItem ,  string accessoriesName , string weight)
    {
     
            container.Q<Label>("nameAccessories").text = text.GetName(true);

            //set icon
            VisualElement groupBoxIcon = container.Q<VisualElement>("GrowIcon");
            VisualElement icon = container.Q<VisualElement>("icon");
            Texture2D texture = IconManager.Instance.LoadTextureByName(etcIcon);
            AddElementIfNotNull(groupBoxIcon, icon, texture);

            VisualElement gradeBox = container.Q<VisualElement>("grade");
            Texture2D grade = text.GetGradeTexture();
            SetImageElement(gradeBox, grade);

            Label labelEnchant = container.Q<Label>("enchant");
            AddElementEnchantifNot0(labelEnchant, labelEnchant, text.GetEnchant());


            Label mpLabel = container.Q<Label>("mpLabel");
            VisualElement mpGroup = container.Q<VisualElement>("mpText");
            AddElementIfNot0(mpGroup, mpLabel, 0);

            Label groupPriceLabel = (Label)container.Q<Label>("PriceName");
            Label priceLabel = (Label)container.Q<Label>("priceLabel");
            AddElementPriceifNot0(groupPriceLabel, priceLabel, priceItem);
            //price.style.color = ToolTipsUtils.GetColorPrice(text.GetDiscription());
            //price.text = ToolTipsUtils.ConvertToPrice(priceItem) + " Adena";


            VisualElement groupBoxMdef = (VisualElement)container.Q<VisualElement>("mdeText");
            Label lebelMdef = (Label)container.Q<Label>("mdefLabel");

            //set mdef
            AddElementIfNot0(groupBoxMdef, lebelMdef, 0);

            //set type
            VisualElement groupTypeLabel = container.Q<VisualElement>("SettingType");
            Label typeLabel = container.Q<Label>("typeLabel");
            AddElementIfNotEmpty(groupTypeLabel, typeLabel, accessoriesName);

            VisualElement groupBoxWeight = container.Q<VisualElement>("weightText");
            Label weightLabel = container.Q<Label>("weightlabel");
            AddElementIfNotEmpty(groupBoxWeight, weightLabel, weight);

            Label descriptionLabel = container.Q<Label>("descriptedLabel");
            AddElementIfNotEmpty(descriptionLabel, descriptionLabel, text.GetItemDiscription());
        //container.Q<Label>("descriptedLabel").text = text.GetItemDiscription();

        
    }

    public void SetRequired(VisualElement container, IDataTips text, string etcIcon, int priceItem, string accessoriesName, string weight , int requiredCount , int countInventory)
    {
        SetOther(container, text, etcIcon, priceItem, accessoriesName, weight);
        SetRequiredField(container , requiredCount , countInventory);
    }

    private void SetRequiredField(VisualElement container, int requiredCount , int inventoryCount)
    {
        Label groupRequiredLabel = (Label)container.Q<Label>("RequredName");
        Label RequiredLabel = (Label)container.Q<Label>("requiredLabel");
        AddElementIfNotEmpty(groupRequiredLabel, RequiredLabel, requiredCount.ToString());


        Label groupInStockLabel = (Label)container.Q<Label>("InStockName");
        Label inStockLabel = (Label)container.Q<Label>("inStockLabel");
        AddElementIfNotEmpty(groupInStockLabel, inStockLabel, inventoryCount.ToString());
        
    }

    protected void AddArmor(VisualElement container, int price, string armorTypeName ,  IDataTips text, Armorgrp armor, VisualTreeAsset setsElements, VisualTreeAsset setsEffects, CreatorSets creator)
    {
        //set icon
        VisualElement groupBoxIcon = container.Q<VisualElement>("GrowIcon");
        
        VisualElement icon = container.Q<VisualElement>("icon");
        Texture2D texture = IconManager.Instance.LoadTextureByName(armor.Icon);
        AddElementIfNotNull(groupBoxIcon, icon, texture);

        container.Q<Label>("nameAccessories").text = text.GetName();

        VisualElement gradeBox = container.Q<VisualElement>("grade");
        Texture2D grade = text.GetGradeTexture();
        SetImageElement(gradeBox, grade);
    

        //VisualElement groupBoxType = container.Q<VisualElement>("typeText");
        //VisualElement groupBoxMDef = container.Q<VisualElement>("mdeText");

        //EnabledRow(groupBoxType);
        //EnabledRow(groupBoxMDef);


        Label labelEnchant = container.Q<Label>("enchant");
        AddElementEnchantifNot0(labelEnchant, labelEnchant, text.GetEnchant());

        VisualElement groupType = container.Q<VisualElement>("typeText");
        Label typeLabel = container.Q<Label>("typeLabel");

        
        Label groupPriceLabel = (Label)container.Q<Label>("PriceName");
        Label priceLabel = (Label)container.Q<Label>("priceLabel");

        AddElementPriceifNot0(groupPriceLabel, priceLabel, price);
        AddElementIfNotEmpty(groupType, typeLabel, armorTypeName);

        Label pdefLabel = container.Q<Label>("pdefLabel");
        VisualElement pdefGroup = container.Q<VisualElement>("pdeText");

        Label mdefLabel = container.Q<Label>("mdefLabel");
        VisualElement mdefGroup = container.Q<VisualElement>("mdeText");

        Label chancedefLabel = container.Q<Label>("chancedefLabel");
        VisualElement chancedefGroup = container.Q<VisualElement>("chancedeText");

        Label dexLabel = container.Q<Label>("dexLabel");
        VisualElement dexGroup = container.Q<VisualElement>("dexText");

        Label mpLabel = container.Q<Label>("mpLabel");
        VisualElement mpGroup = container.Q<VisualElement>("mpText");



        AddElementIfNot0(mpGroup, mpLabel, armor.MpBonus);
        AddElementIfNot0(pdefGroup, pdefLabel, armor.PDef);
        AddElementIfNot0(mdefGroup, mdefLabel, armor.MDef);
        AddElementIfNot0(chancedefGroup, chancedefLabel, 0);
        AddElementIfNot0(dexGroup, dexLabel, 0);
        AddElementIfNot0(dexGroup, dexLabel, 0);

        VisualElement footer_container = container.Q<VisualElement>("Footer");

        creator.AddSetsElement(footer_container, setsElements, setsEffects , text.GetSets() , text.GetSetsEffect());


        container.Q<Label>("weightlabel").text = armor.Weight.ToString();

        Label descriptionLabel = container.Q<Label>("descriptedLabel");
        AddElementIfNotEmpty(descriptionLabel, descriptionLabel, text.GetItemDiscription());

        //container.Q<Label>("descriptedLabel").text = text.GetItemDiscription();
    }

    protected void AddArmorShield(VisualElement container, int price, string armorTypeName , IDataTips text, Weapongrp armor)
    {
        //set icon
        VisualElement groupBoxIcon = container.Q<VisualElement>("GrowIcon");
        VisualElement icon = container.Q<VisualElement>("icon");
        Texture2D texture = IconManager.Instance.LoadTextureByName(armor.Icon);
        AddElementIfNotNull(groupBoxIcon, icon, texture);

        container.Q<Label>("nameAccessories").text = text.GetName();

        VisualElement gradeBox = container.Q<VisualElement>("grade");
        Texture2D grade = text.GetGradeTexture();
        SetImageElement(gradeBox, grade);

        VisualElement groupBoxType = container.Q<VisualElement>("typeText");
        VisualElement groupBoxMDef = container.Q<VisualElement>("mdeText");

        EnabledRow(groupBoxType);
        EnabledRow(groupBoxMDef);



        VisualElement groupType = container.Q<VisualElement>("typeText");
        Label typeLabel = container.Q<Label>("typeLabel");

        Label groupPriceLabel = (Label)container.Q<Label>("PriceName");
        Label priceLabel = (Label)container.Q<Label>("priceLabel");

        Label labelEnchant = container.Q<Label>("enchant");
        AddElementEnchantifNot0(labelEnchant, labelEnchant, text.GetEnchant());

        AddElementPriceifNot0(groupPriceLabel, priceLabel, price);
        AddElementIfNotEmpty(groupType, typeLabel, armorTypeName);

        Label pdefLabel = container.Q<Label>("pdefLabel");
        VisualElement pdefGroup = container.Q<VisualElement>("pdeText");

        Label mdefLabel = container.Q<Label>("mdefLabel");
        VisualElement mdefGroup = container.Q<VisualElement>("mdeText");

        Label chancedefLabel = container.Q<Label>("chancedefLabel");
        VisualElement chancedefGroup = container.Q<VisualElement>("chancedeText");

        Label dexLabel = container.Q<Label>("dexLabel");
        VisualElement dexGroup = container.Q<VisualElement>("dexText");

        Label mpLabel = container.Q<Label>("mpLabel");
        VisualElement mpGroup = container.Q<VisualElement>("mpText");
        AddElementIfNot0(mpGroup, mpLabel, 0);

        AddElementIfNot0(pdefGroup, pdefLabel, armor.ShieldPdef);
        AddElementIfNot0(mdefGroup, mdefLabel, 0);
        AddElementIfNot0(chancedefGroup, chancedefLabel, armor.ShieldRate);
        AddElementIfNot0(dexGroup, dexLabel, armor.Dex);

        container.Q<Label>("weightlabel").text = armor.Weight.ToString();

        Label descriptionLabel = container.Q<Label>("descriptedLabel");
        AddElementIfNotEmpty(descriptionLabel, descriptionLabel, text.GetItemDiscription());


        //container.Q<Label>("descriptedLabel").text = text.GetItemDiscription();
    }

    protected void SetDataIngredient(VisualElement container, IDataTips text, string icon, int count, string name)
    {

        VisualElement gradeBox = container.Q<VisualElement>("grade");
        Texture2D grade = text.GetGradeTexture();
        SetImageElement(gradeBox, grade);

        VisualElement groupIcon = container.Q<VisualElement>("GrowIcon");
        var iconElement = container.Q<VisualElement>("icon");
        AddElementIfNotNull(groupIcon, iconElement, IconManager.Instance.LoadTextureByName(icon));

        VisualElement groupPriceLabel = container.Q<VisualElement>("GrowPrice");
        Label priceLabel = (Label)container.Q<Label>("price");
        AddElementCountXifNot0(groupPriceLabel, priceLabel, count);

        VisualElement groupName = container.Q<VisualElement>("GrowName");
        Label labelName = container.Q<Label>("nameLabel");
        AddElementIfNotEmpty(groupName, labelName, name);
    }

}
