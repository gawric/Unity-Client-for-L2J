using UnityEngine;

public class CharacterBuilder : MonoBehaviour
{
    private static CharacterBuilder _instance;
    public static CharacterBuilder Instance { get { return _instance; } }

    void Awake() {
        if (_instance == null) {
            _instance = this;
        } else if (_instance != this) {
            Destroy(this);
        }
    }

    // Load player animations, face and hair
    public GameObject BuildCharacterBase(CharacterRaceAnimation raceId, PlayerAppearance appearance, EntityType entityType) {
        //Debug.Log($"Building character: Race:{raceId} Sex:{appearance.Sex} Face:{appearance.Face} Hair:{appearance.HairStyle} HairC:{appearance.HairColor}");

        GameObject prototype = ModelTable.Instance.GetContainer(raceId, entityType);
        if (prototype == null) {
            // m0nster: временная заглушка, пока не реализованы все персонажи
            return null;
        }
        
        GameObject entity = Instantiate(prototype);
        Transform container = entityType != EntityType.Player ? 
            entity.transform.GetChild(0) : 
            entity.transform;
        GameObject face = Instantiate(ModelTable.Instance.GetFace(raceId, appearance.Face), container, false);
        GameObject hair1 = Instantiate(ModelTable.Instance.GetHair(raceId, appearance.HairStyle, appearance.HairColor, false), container, false);
        GameObject hair2 = Instantiate(ModelTable.Instance.GetHair(raceId, appearance.HairStyle, appearance.HairColor, true), container, false);

        UserGear gear = entity.GetComponent<UserGear>();
        gear.AddUserGearLink(face, hair1, hair2);

        return entity;
    }

    public GameObject ReplaceNewPawnFace(CharacterRaceAnimation raceId , byte _face , byte hairColor , byte hairStyle) {
        //Debug.Log($"Building character: Race:{raceId} Sex:{appearance.Sex} Face:{appearance.Face} Hair:{appearance.HairStyle} HairC:{appearance.HairColor}");
        GameObject entity = Instantiate(ModelTable.Instance.GetContainer(raceId, EntityType.Pawn));
        
        Transform container = entity.transform;
        Instantiate(ModelTable.Instance.GetFace(raceId, _face), container, false);
        Instantiate(ModelTable.Instance.GetHair(raceId, hairStyle, hairColor, false), container, false);
        Instantiate(ModelTable.Instance.GetHair(raceId, hairStyle, hairColor, true), container, false);

        return entity;
    }
    
    public GameObject BuildCharacterBaseInterlude(CharacterRaceAnimation raceId, PlayerInterludeAppearance appearance, EntityType entityType) {
        //Debug.Log($"Building character: Race:{raceId} Sex:{appearance.Sex} Face:{appearance.Face} Hair:{appearance.HairStyle} HairC:{appearance.HairColor}");
        GameObject prototype = ModelTable.Instance.GetContainer(raceId, entityType);
        if (prototype == null) {
            // m0nster: временная заглушка, пока не реализованы все персонажи
            return null;
        }
        
        GameObject entity = Instantiate(prototype);
        Transform container = entityType != EntityType.Player ? 
            entity.transform.GetChild(0) : 
            entity.transform;
        
        Instantiate(ModelTable.Instance.GetFace(raceId, appearance.FaceByte), container, false);
        Instantiate(ModelTable.Instance.GetHair(raceId, appearance.HairStyleByte, appearance.HairColorByte, false), container, false);
        Instantiate(ModelTable.Instance.GetHair(raceId, appearance.HairStyleByte, appearance.HairColorByte, true), container, false);

        return entity;
    }

    private GameObject GetHair() {
        return null;
    }
 
}
