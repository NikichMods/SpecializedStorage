using System.Collections.Generic;
using UnityEngine;

public class WorldGameObject
{
    public string obj_id;
    public Item data { get { return null; } }
}

public class Item
{
    public string id;
    public int value;
    public List<Item> inventory;
    public List<string> multiquality_items;
    public ItemDefinition definition { get { return null; } }
}

public class ItemDefinition
{
    public enum AlchemyType
    {
        None = 0
    }

    public enum BagType
    {
        Alchemy = 2,
        Potions = 6,
        Food = 8
    }

    public enum ItemType
    {
        Preach = 20,
        BodyUniversalPart = 270
    }

    public AlchemyType alch_type;
    public List<BagType> can_be_inserted_in_bag;
    public ItemType type;
    public int stack_count;
}

public class CraftDefinition
{
    public enum CraftType
    {
        MixedCraft = 3,
        AlchemyDecompose = 5
    }

    public CraftType craft_type;
    public List<Item> needs;
    public List<Item> output;
    public List<string> craft_in;
}

public class GameBalance
{
    public static GameBalance me { get { return null; } }
    public List<CraftDefinition> craft_data;
}

public class ChestGUI : MonoBehaviour
{
    public InventoryPanelGUI chest_panel;
}

public class InventoryPanelGUI : MonoBehaviour
{
}

public class BaseItemCellGUI : MonoBehaviour
{
    public BaseItemCellElements container;
}

public class BaseItemCellElements
{
    public UI2DSprite back;
    public UI2DSprite icon;
    public UI2DSprite selection;
}
