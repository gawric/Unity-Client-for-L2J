using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using UnityEngine;
using static StorageVariable;

public class StorageVariable
{
    private static StorageVariable instance;
    private object _sync = new object();
    private static VariableItem[] variables;
    private static int size = 10;
    private static ManualResetEvent _resetMessageDelay;
    private static int _refreshMessageId;
    //moderate storage of $1 of requested active variables

    public enum MessageID
    {
        ADD_INVENTORY = 52,
        ADD_EXP_SP = 95,
        ENCHANT_FAIL = 65,
        USE_SKILL = 936,
        NOT_HAVE_ADENA = 279,
        NOT_ENOUGH_SP_TO_LEARN_SKILL = 278,
        NO_ITEM_DEPOSITED_IN_WH = 282,
        LEARNED_SKILL_S1 = 277,
        ITEM_MISSING_TO_LEARN_SKILL = 276,
        
    }


    private StorageVariable()
    { }

    public static StorageVariable getInstance()
    {
        if (instance == null)
        {
            instance = new StorageVariable();
            variables = new VariableItem[size];
            _refreshMessageId = -1;
        }

        return instance;
    }
    public  void SetManualMessage(ManualResetEvent resetMessageDelay)
    {
        _resetMessageDelay = resetMessageDelay;
    }



    public  void ResumeShowDelayMessage(int MessageId)
    {
        //lock (_sync)
        //{
            _refreshMessageId = MessageId;
            _resetMessageDelay.Set();
       // }
           
    }

    public int GetMessageIdResume()
    {
        lock (_sync)
        {
            return _refreshMessageId;
        }
            
    }
    public void AddS1Items(VariableItem itemsParce)
    {
            variables[0] = itemsParce;
    }
    public void AddS2Items(VariableItem itemsParce)
    {
        //lock (_sync)
        //{
            variables[1] = itemsParce;
       // }
    }

    public void AddРЎ1Items(VariableItem itemsParce)
    {
       // lock (_sync)
       // {
            variables[3] = itemsParce;
        //}
    }

    public VariableItem GetVariable(int index)
    {
        //lock (_sync)
        //{
            if(index >= 10) return null;

            if (variables[index] != null)
            {
                return variables[index];
            }
            return null;
       //// }

    }

    public VariableItem GetVariableByName(string l2j)
    {
        //lock (_sync)
        //{
            if (l2j.Equals("$s1")) {
                return variables[0];
            }else if (l2j.Equals("$s2"))
            {
                return variables[1];
            }
            else if (l2j.Equals("$c1"))
            {
                return variables[3];
            }
            return null;
       // }

    }



}
