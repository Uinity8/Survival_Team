using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CraftTable : MonoBehaviour
{
    //ItemData Scriptable Item¿¡ bool isCraftable À¸·Î 
    //enum ItemType { Tool, Housing, }

    protected CraftItemData itemData;
    //protected List<ItemData> craftTypeData;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    //List<ItemData> GetCraftableType(ItemType type)
    //{
    //    return itemData.FindAll(item => item.type == type);
    //}
}
