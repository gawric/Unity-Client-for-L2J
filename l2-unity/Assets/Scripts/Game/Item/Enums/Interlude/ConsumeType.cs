using UnityEngine;

public class ConsumeType 
{
    public static ConsumeCategory ParceCategory(string type2)
    {
        if (type2.Equals("consume_type_stackable"))
        {
            return ConsumeCategory.Stackable;
        }
        else if (type2.Equals("consume_type_normal"))
        {
            return ConsumeCategory.Normal;
        }
        else if (type2.Equals("consume_type_asset"))
        {
            return ConsumeCategory.Asset;
        }


        return ConsumeCategory.Normal;

    }
}
