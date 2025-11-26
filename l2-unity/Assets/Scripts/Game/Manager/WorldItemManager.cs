//using UnityEngine;
//using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

//public class WorldItemManager : MonoBehaviour
//{
//    private static WorldItemManager _instance;

//    public static WorldItemManager Instance {
//        get
//        {
//            if (_instance == null)
//            {
//                _instance = new WorldItemManager();
//            }

//            return _instance;
//        }
//    }

//    [SerializeField] private Transform worldItemsRoot; // пустой объект в сцене, куда класть дропы

//    //private readonly Dictionary<int, WorldItem> _items = new();

//    private void Awake()
//    {
//        //Instance = this;
//    }

//    public void SpawnItem(int objectId, int itemId, Vector3 position, int count)
//    {
//        var itemModel = ModelTable.Instance.GetWeapon("LineageWeapons.long_sword_m00_wp");

//        GameObject itemGo = Instantiate(itemModel, position, Quaternion.identity);
        

//        //var itemDef = ItemDatabase.Instance.Get(itemId); // или как у тебя устроена база
//        //var prefab = itemDef.WorldPrefab;

//        //if (prefab == null)
//        //{
//        //    Debug.LogWarning($"No world prefab for itemId {itemId}, spawning cube instead");
//        //    prefab = DefaultCubePrefab; // на первое время можно просто куб
//        //}

//        //var go = Instantiate(prefab, position, Quaternion.identity, worldItemsRoot);
//        //var worldItem = go.GetComponent<WorldItem>();
//        //worldItem.Init(objectId, itemId, count);

//        //_items[objectId] = worldItem;
//    }

//    public void RemoveItem(int objectId)
//    {
//        //if (_items.TryGetValue(objectId, out var worldItem))
//        //{
//        //    Destroy(worldItem.gameObject);
//        //    _items.Remove(objectId);
//        //}
//    }
//}