using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Floating names above character-select pawns (login lobby).
/// Driven by <see cref="CharacterSelector"/> pawns — keyed by list index
/// (server ObjId in CharSelectionInfo can collide across slots).
/// </summary>
public class LobbyNameplatesManager : MonoBehaviour
{
    private VisualElement _rootElement;
    private VisualTreeAsset _nameplateTemplate;
    private readonly Dictionary<int, Nameplate> _nameplates = new Dictionary<int, Nameplate>();

    [SerializeField] private Camera _camera;
    [SerializeField] private float _nameplateViewDistance = 80f;

    public Camera Camera { get { return _camera; } set { _camera = value; } }

    private static LobbyNameplatesManager _instance;
    public static LobbyNameplatesManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        _nameplates.Clear();
        _instance = null;
    }

    void Start()
    {
        if (_nameplateTemplate == null)
        {
            _nameplateTemplate = Resources.Load<VisualTreeAsset>("Data/UI/_Elements/Game/Nameplate");
        }
        if (_nameplateTemplate == null)
        {
            Debug.LogError("LobbyNameplatesManager: could not load Nameplate UXML.");
        }
    }

    private const int kUpdatesPerSecond = 60;
    private const float kUpdateInterval = 1.0f / kUpdatesPerSecond;
    private float _accumulation;

    private void Update()
    {
        _accumulation += Time.deltaTime;
        while (_accumulation >= kUpdateInterval)
        {
            UpdateNameplatePositions();
            _accumulation -= kUpdateInterval;
        }
    }

    private void FixedUpdate()
    {
        if (_camera == null)
        {
            ClearNameplates();
            return;
        }

        if (L2LoginUI.Instance == null || !L2LoginUI.Instance.UILoaded)
        {
            return;
        }

        if (_rootElement == null)
        {
            _rootElement = L2LoginUI.Instance.RootElement.Q<VisualElement>("NameplatesContainer");
            return;
        }

        if (_nameplateTemplate == null)
        {
            return;
        }

        SyncNameplatesFromSelector();
    }

    private void SyncNameplatesFromSelector()
    {
        if (CharacterSelector.Instance == null)
        {
            return;
        }

        IReadOnlyList<GameObject> pawns = CharacterSelector.Instance.CharacterPawns;
        if (pawns == null)
        {
            return;
        }

        var aliveKeys = new HashSet<int>();

        for (int i = 0; i < pawns.Count; i++)
        {
            GameObject pawn = pawns[i];
            if (pawn == null)
            {
                continue;
            }

            SelectableCharacterEntity entity = pawn.GetComponent<SelectableCharacterEntity>();
            if (entity == null || entity.CharacterInfoInterlude == null)
            {
                continue;
            }

            CharSelectInfoPackage info = entity.CharacterInfoInterlude;
            int plateKey = i;
            aliveKeys.Add(plateKey);

            bool visible = IsNameplateVisible(entity.transform);

            if (!_nameplates.ContainsKey(plateKey))
            {
                CreateNameplate(entity, plateKey);
                continue;
            }

            Nameplate existing = _nameplates[plateKey];
            if (!string.Equals(existing.Name, info.Name, System.StringComparison.Ordinal))
            {
                existing.Name = info.Name;
                Label label = existing.NameplateEle.Q<Label>("EntityName");
                if (label != null)
                {
                    label.text = info.Name;
                }
            }

            existing.Visible = visible;
            existing.NameplateEle.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        var toRemove = new List<int>();
        foreach (int id in _nameplates.Keys)
        {
            if (!aliveKeys.Contains(id))
            {
                toRemove.Add(id);
            }
        }

        for (int r = 0; r < toRemove.Count; r++)
        {
            int id = toRemove[r];
            _nameplates[id].NameplateEle.RemoveFromHierarchy();
            _nameplates.Remove(id);
        }
    }

    private void CreateNameplate(SelectableCharacterEntity entity, int plateKey)
    {
        if (!IsNameplateVisible(entity.transform))
        {
            return;
        }

        float height = CharacterHeight.GetHeight(entity.CharacterInfoInterlude.CharacterRaceAnimation);
        VisualElement visualElement = _nameplateTemplate.Instantiate()[0];

        Nameplate nameplate = new Nameplate(
            visualElement,
            visualElement.Q<Label>("EntityName"),
            visualElement.Q<Label>("EntityTitle"),
            entity.transform,
            "",
            "9CE8A9FF",
            height,
            entity.CharacterInfoInterlude.Name,
            plateKey,
            true);

        _nameplates[plateKey] = nameplate;
        _rootElement.Add(visualElement);
    }

    private void UpdateNameplatePositions()
    {
        if (_camera == null || _rootElement == null)
        {
            return;
        }

        foreach (Nameplate nameplate in _nameplates.Values)
        {
            if (nameplate == null || !nameplate.Visible || nameplate.Target == null)
            {
                continue;
            }

            UpdateNameplatePosition(nameplate);
        }
    }

    private void UpdateNameplatePosition(Nameplate nameplate)
    {
        try
        {
            Vector3 world = nameplate.Target.position + Vector3.up * nameplate.NameplateOffsetHeight;
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (screen.z < 0f)
            {
                nameplate.NameplateEle.style.display = DisplayStyle.None;
                return;
            }

            nameplate.NameplateEle.style.display = DisplayStyle.Flex;
            nameplate.NameplateEle.style.left = screen.x - nameplate.NameplateEle.resolvedStyle.width / 2f;
            nameplate.NameplateEle.style.top = Screen.height - screen.y - nameplate.NameplateEle.resolvedStyle.height;
        }
        catch (System.NullReferenceException) { }
        catch (MissingReferenceException) { }
    }

    private bool IsNameplateVisible(Transform target)
    {
        if (target == null || _camera == null)
        {
            return false;
        }

        return Vector3.Distance(_camera.transform.position, target.position) <= _nameplateViewDistance;
    }

    private void ClearNameplates()
    {
        foreach (Nameplate nameplate in _nameplates.Values)
        {
            nameplate.NameplateEle.RemoveFromHierarchy();
        }

        _nameplates.Clear();
    }
}
