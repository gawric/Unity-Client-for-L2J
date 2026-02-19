using UnityEngine;

public class ParseEdit : IElementsUI
{
    private string _varName;
    public ParseEdit(string varName)
    {
        _varName = varName;
    }

    public string GetVarName()
    {
        return _varName;
    }
}
