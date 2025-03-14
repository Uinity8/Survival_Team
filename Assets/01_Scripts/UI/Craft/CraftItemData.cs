using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftItemData : MonoBehaviour
{
    public ItemDatabase itemDatabase;
    List<CraftItemData> craftItems;

    void Start()
    {
        //제작 가능 아이템들만 가져오기
        //craftItems = itemDatabase.GetCraftableItems();
    }

    void Update()
    {
        
    }
}
