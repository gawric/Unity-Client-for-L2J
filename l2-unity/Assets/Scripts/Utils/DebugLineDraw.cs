using UnityEngine;

public class DebugLineDraw 
{
    public static void ShowDrawLineDebug(int objId, Vector3 source, Vector3 targetObj, Color color)
    {
        var _debugObject = GameObject.FindWithTag("DebugMove");
        var _debugLine = _debugObject.GetComponent<DrawLine>();

        //var vector1 = new Vector3(transform.x, transform.y, transform.z);
        //var vector2 = new Vector3(targetObj.x, transform.y, targetObj.z);
       // _debugLine.DrawNewLineV2(objId, source, targetObj, color);
    }

    public static void ShowDrawLineDebugNpc(int objId, Vector3 source, Vector3 targetObj, Color color)
    {
        var _debugObject = GameObject.FindWithTag("DebugMove");
        var _debugLine = _debugObject.GetComponent<DrawLine>();

        //var vector1 = new Vector3(transform.x, transform.y, transform.z);
        //var vector2 = new Vector3(targetObj.x, transform.y, targetObj.z);
         _debugLine.DrawNewLineV2(objId, source, targetObj, color);
    }

    public static void RemoveDrawLineDebug(int objId)
    {
        var _debugObject = GameObject.FindWithTag("DebugMove");
        var _debugLine = _debugObject.GetComponent<DrawLine>();

        //var vector1 = new Vector3(transform.x, transform.y, transform.z);
        //var vector2 = new Vector3(targetObj.x, transform.y, targetObj.z);
        //_debugLine.RemoveDebugLines(objId);
    }
}
