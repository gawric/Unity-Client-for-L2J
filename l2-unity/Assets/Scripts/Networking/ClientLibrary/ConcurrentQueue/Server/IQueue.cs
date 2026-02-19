using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IQueue
{
    void AddItem(ItemServer item);
    void Dispose();

}
