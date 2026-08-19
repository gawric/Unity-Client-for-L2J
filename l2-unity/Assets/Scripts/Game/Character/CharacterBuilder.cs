using UnityEngine;
using VContainer;

public class CharacterBuilder : MonoBehaviour
{
    private static CharacterBuilder _instance;
    public static CharacterBuilder Instance { get { return _instance; } }

    [Inject] private ModelTable _models;

    private ModelTable Models
    {
        get { return _models != null ? _models : ModelTable.Instance; }
    }

    void Awake() {
        if (_instance == null) {
            _instance = this;
        } else if (_instance != this) {
            Destroy(this);
        }
    }

    // Load player animations, face and hair
    public GameObject BuildCharacterBase(CharacterRaceAnimation raceId, PlayerAppearance appearance, EntityType entityType) {
        GameObject prototype = Models.GetContainer(raceId, entityType);
        if (prototype == null) {
            return null;
        }

        GameObject entity = Instantiate(prototype);
        Transform container = entityType != EntityType.Player ?
            entity.transform.GetChild(0) :
            entity.transform;
        GameObject face = Instantiate(Models.GetFace(raceId, appearance.FaceByte), container, false);
        GameObject hair1 = Instantiate(Models.GetHair(raceId, appearance.HairStyleByte, appearance.HairColorByte, false), container, false);
        GameObject hair2 = Instantiate(Models.GetHair(raceId, appearance.HairStyleByte, appearance.HairColorByte, true), container, false);

        UserGear gear = entity.GetComponent<UserGear>();
        gear.AddUserGearLink(face, hair1, hair2);

        return entity;
    }

    public GameObject ReplaceNewPawnFace(CharacterRaceAnimation raceId , byte _face , byte hairColor , byte hairStyle) {
        GameObject entity = Instantiate(Models.GetContainer(raceId, EntityType.Pawn));

        Transform container = entity.transform;
        Instantiate(Models.GetFace(raceId, _face), container, false);
        Instantiate(Models.GetHair(raceId, hairStyle, hairColor, false), container, false);
        Instantiate(Models.GetHair(raceId, hairStyle, hairColor, true), container, false);

        return entity;
    }

    public GameObject BuildCharacterBaseInterlude(CharacterRaceAnimation raceId, PlayerAppearance appearance, EntityType entityType) {
        GameObject prototype = Models.GetContainer(raceId, entityType);
        if (prototype == null) {
            return null;
        }

        GameObject entity = Instantiate(prototype);
        Transform container = entityType != EntityType.Player ?
            entity.transform.GetChild(0) :
            entity.transform;

        Instantiate(Models.GetFace(raceId, appearance.FaceByte), container, false);
        Instantiate(Models.GetHair(raceId, appearance.HairStyleByte, appearance.HairColorByte, false), container, false);
        Instantiate(Models.GetHair(raceId, appearance.HairStyleByte, appearance.HairColorByte, true), container, false);

        return entity;
    }


}
