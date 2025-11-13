using UnityEngine;

public class RecipeMaterials
{
    private int materials_m_id;
    private int materials_m_cnt;

    // Constructor
    public RecipeMaterials(int id, int count)
    {
        materials_m_id = id;
        materials_m_cnt = count;
    }

    // Getter and Setter for materials_m_id
    public int MaterialsMId
    {
        get { return materials_m_id; }
        set { materials_m_id = value; }
    }

    // Getter and Setter for materials_m_cnt
    public int MaterialsMCnt
    {
        get { return materials_m_cnt; }
        set { materials_m_cnt = value; }
    }
}
