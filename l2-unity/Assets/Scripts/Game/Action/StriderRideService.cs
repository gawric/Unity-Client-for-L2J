using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// RiderEnter spawns a new LineageMonster.strider and AttachToBone(player, Bone15).
/// The summoned pet stays in the world on the server; here it is hidden while the
/// player sits, then shown again. RiderEnd destroys only the mount the player sat on.
/// </summary>
public sealed class StriderRideService : ILateTickable
{
    const string PrefabFolder = "Data/Animations/LineageMonsters/strider_m00";
    const string SeatBone = "Bone15";

    public static StriderRideService Instance { get; private set; }

    struct SavedChild
    {
        public Transform Child;
        public Transform Parent;
        public int Sibling;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }

    sealed class Seat
    {
        public Entity Rider;
        public GameObject Mount;
        public Transform Bone;
        public Animator Animator;
        public bool Running;
        public readonly List<SavedChild> Children = new List<SavedChild>();
        public readonly List<GameObject> HiddenPets = new List<GameObject>();
    }

    readonly Dictionary<int, Seat> _seats = new Dictionary<int, Seat>();
    readonly List<int> _gone = new List<int>();

    public StriderRideService()
    {
        Instance = this;
    }

    public bool IsRiding(Entity rider)
    {
        return rider != null && rider.Identity != null && _seats.ContainsKey(rider.Identity.Id);
    }

    public void LateTick()
    {
        if (_seats.Count == 0)
            return;

        _gone.Clear();
        foreach (KeyValuePair<int, Seat> pair in _seats)
        {
            Seat seat = pair.Value;
            if (seat.Rider == null || seat.Mount == null)
            {
                _gone.Add(pair.Key);
                continue;
            }
            Place(seat);
        }

        for (int i = 0; i < _gone.Count; i++)
            Dismount(_gone[i]);
    }

    public bool TryMount(Entity rider)
    {
        if (rider == null || rider.Identity == null)
            return false;
        int id = rider.Identity.Id;
        if (_seats.ContainsKey(id))
            return true;

        GameObject prefab = LoadStrider();
        if (prefab == null)
            return false;

        GameObject mount = Object.Instantiate(prefab);
        mount.name = "strider_ride";
        DisableGameplay(mount);
        Transform bone = FindChild(mount.transform, SeatBone);
        if (bone == null)
        {
            Object.Destroy(mount);
            return false;
        }

        Seat seat = new Seat
        {
            Rider = rider,
            Mount = mount,
            Bone = bone,
            Animator = mount.GetComponentInChildren<Animator>()
        };
        if (seat.Animator != null)
        {
            seat.Animator.Play("wait", 0, 0f);
            seat.Animator.Update(0f);
        }
        Place(seat);
        AttachChildren(seat);
        _seats.Add(id, seat);
        HideOwnedStriders(seat);
        return true;
    }

    public void Dismount(Entity rider)
    {
        if (rider == null || rider.Identity == null)
            return;
        Dismount(rider.Identity.Id);
    }

    public void SetMountGait(int objectId, bool running)
    {
        Seat seat;
        if (!_seats.TryGetValue(objectId, out seat) || seat.Animator == null || seat.Running == running)
            return;
        seat.Running = running;
        seat.Animator.CrossFade(running ? "run" : "wait", 0.12f, 0);
    }

    void Dismount(int id)
    {
        Seat seat;
        if (!_seats.TryGetValue(id, out seat))
            return;
        _seats.Remove(id);
        SnapBodyToGroundPose(seat);
        RestoreChildren(seat);
        for (int i = 0; i < seat.HiddenPets.Count; i++)
        {
            GameObject hidden = seat.HiddenPets[i];
            if (hidden == null)
                continue;
            hidden.SetActive(true);
            StandPet(hidden.GetComponent<Entity>());
        }
        if (seat.Mount != null)
        {
            seat.Mount.SetActive(false);
            Object.Destroy(seat.Mount);
        }
    }

    static void HideOwnedStriders(Seat seat)
    {
        Entity[] entities = Object.FindObjectsByType<Entity>(FindObjectsSortMode.None);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entity == null || entity.Identity == null || entity == seat.Rider)
                continue;
            if (seat.Mount != null && entity.gameObject == seat.Mount)
                continue;
            if (!entity.Identity.Owned || !entity.gameObject.activeInHierarchy)
                continue;
            int npcId = entity.Identity.NpcId;
            if (npcId != 12526 && npcId != 12527 && npcId != 12528)
                continue;
            if (MoveAllCharacters.Instance != null)
                MoveAllCharacters.Instance.Forget(entity.Identity.Id);
            entity.gameObject.SetActive(false);
            seat.HiddenPets.Add(entity.gameObject);
        }
    }

    static void StandPet(Entity pet)
    {
        if (pet == null)
            return;
        if (pet.Identity != null && MoveAllCharacters.Instance != null)
            MoveAllCharacters.Instance.Forget(pet.Identity.Id);
        if (EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.Set(pet, EntityActionKind.Idle, null);
        Animator animator = pet.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return;
        animator.Play("wait", 0, 0f);
        animator.Update(0f);
    }

    static void Place(Seat seat)
    {
        Transform rider = seat.Rider.transform;
        Quaternion yaw = Quaternion.Euler(0f, rider.eulerAngles.y, 0f);
        seat.Mount.transform.SetPositionAndRotation(rider.position, yaw);
    }

    static void AttachChildren(Seat seat)
    {
        Transform rider = seat.Rider.transform;
        Quaternion yaw = Quaternion.Euler(0f, rider.eulerAngles.y, 0f);
        for (int i = rider.childCount - 1; i >= 0; i--)
        {
            Transform child = rider.GetChild(i);
            if (child == seat.Mount.transform)
                continue;
            Vector3 worldScale = child.lossyScale;
            seat.Children.Add(new SavedChild
            {
                Child = child,
                Parent = rider,
                Sibling = child.GetSiblingIndex(),
                LocalPosition = child.localPosition,
                LocalRotation = child.localRotation,
                LocalScale = child.localScale
            });
            child.SetParent(seat.Bone, false);
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.Inverse(seat.Bone.rotation) * yaw;
            Vector3 seatScale = seat.Bone.lossyScale;
            child.localScale = new Vector3(
                DivideScale(worldScale.x, seatScale.x),
                DivideScale(worldScale.y, seatScale.y),
                DivideScale(worldScale.z, seatScale.z));
        }
    }

    static void SnapBodyToGroundPose(Seat seat)
    {
        // RiderEnd clears the ride flag and plays GetCurWaitAnimName on the body.
        // The sit clip keeps the hips on Bone15. The player root is the feet, so the
        // standing wait is sampled before the body is parented back.
        Animator body = FindBodyAnimator(seat);
        if (body == null)
            return;

        string state = "wait_1HS";
        AnimationManager animations = AnimationManager.Instance as AnimationManager;
        if (animations != null && seat.Rider != null && seat.Rider.Identity != null)
        {
            string final = animations.GetFinalNameAnim(seat.Rider.Identity.Id, AnimationNames.WAIT.Value);
            if (PlayerLocomotionAnim.TryResolve(final, out string resolved, out _))
                state = resolved;
        }

        body.Play(state, 0, 0f);
        body.Update(0f);
    }

    static Animator FindBodyAnimator(Seat seat)
    {
        for (int i = 0; i < seat.Children.Count; i++)
        {
            Transform child = seat.Children[i].Child;
            if (child == null)
                continue;
            Animator body = child.GetComponentInChildren<Animator>();
            if (body != null)
                return body;
        }

        return seat.Rider != null ? seat.Rider.GetComponentInChildren<Animator>() : null;
    }

    static void RestoreChildren(Seat seat)
    {
        for (int i = seat.Children.Count - 1; i >= 0; i--)
        {
            SavedChild saved = seat.Children[i];
            if (saved.Child == null)
                continue;
            saved.Child.SetParent(saved.Parent, false);
            saved.Child.SetSiblingIndex(saved.Sibling);
            saved.Child.localPosition = saved.LocalPosition;
            saved.Child.localRotation = saved.LocalRotation;
            saved.Child.localScale = saved.LocalScale;
        }
    }

    static float DivideScale(float world, float parent)
    {
        if (Mathf.Abs(parent) < 0.0001f)
            return world;
        return world / parent;
    }

    static GameObject LoadStrider()
    {
        GameObject[] loaded = Resources.LoadAll<GameObject>(PrefabFolder);
        GameObject best = null;
        for (int i = 0; i < loaded.Length; i++)
        {
            GameObject go = loaded[i];
            if (go == null || go.name != "strider_m00")
                continue;
            if (go.GetComponent<Animator>() != null)
                return go;
            best = go;
        }
        return best;
    }

    static void DisableGameplay(GameObject mount)
    {
        Behaviour[] behaviours = mount.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is Animator)
                continue;
            behaviours[i].enabled = false;
        }

        Collider[] colliders = mount.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    static Transform FindChild(Transform root, string bone)
    {
        if (root.name == bone)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChild(root.GetChild(i), bone);
            if (found != null)
                return found;
        }
        return null;
    }
}