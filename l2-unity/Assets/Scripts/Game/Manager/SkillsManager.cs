using UnityEngine;


public class SkillsManager : MonoBehaviour
{
    private static SkillsManager _instance;

    public static SkillsManager Instance { get { return _instance; } }

    private AnimationCombo _animCombo;
    private int _hitTime;
    private float _hitTimeSec;
    private int _targetObjId;
    private float _dist;

    private float _elapsedTime = 0;
    private bool _enabled = false;
    private int _complete = -1;
    private EffectSkillsmanager _effectManager;
    private float _starttime = 0;
    private ICombo _comboSkill;
    private BothModel _bothModel;
    private bool _isCalCulatedStart = false;
    private bool _isCalCulatedUpdate = false;
    private bool _isCalCulatedFinish = false;



    public void Start()
    {
        
    }


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this);
        }

    }

    public void Update()
    {
        
    }

    /// <summary>
    /// The following skills need to be measured by time and some formula should be derived on how to convert time into animation cast
    /// Now it’s so bad that the animation works for about as long as the 4 sec WindStrike server requests
    /// </summary>
    void FixedUpdate()
    {
        if (!_enabled) return;

        _elapsedTime = (Time.time * 1000) - _starttime;
        EffectSkillsmanager.Instance.UpdateWorldTime(_elapsedTime);

        if(_elapsedTime >_hitTime * 2 ) _enabled = false;

        if (_animCombo == null) return;

        switch (_complete)
        {
            case -1:
                HandleStartEvent();
                break;
            case 0:
                HandleUpdateEvent();
                break;
            case 1:
                HandleFinishEvent();
                break;
            case 2:
                HandleFlyEvent();
                break;
            case 3:
                _enabled = false;
                break;
        }

    }

    private void HandleStartEvent()
    {
        SetCurrentAnim(_animCombo.GetAnimToIndex(0));
        EventOnly1TimeStart();
        _complete = Start(_comboSkill, _elapsedTime, _bothModel);
    }

    private void HandleUpdateEvent()
    {
        SetCurrentAnim(_animCombo.GetAnimToIndex(1));
        EventOnly1TimeUpdate();
        _complete = Update(_comboSkill, _elapsedTime, _bothModel);
    }

    private void HandleFinishEvent()
    {
        SetCurrentAnim(_animCombo.GetAnimToIndex(2));
        EventOnly1TimeFinish();
        _complete = Final(_comboSkill, _elapsedTime, _bothModel);
        //FinishEvent(_animCombo.GetAnimToIndex(2), ref _singl_run3, _targetObjId, ref _elapsedTime);
    }

    private void HandleFlyEvent()
    {
        if (_bothModel.isFly())
        {
            _bothModel.SetIsFly(false);
            _complete = Fly(_comboSkill, _elapsedTime, _bothModel);
        }
        
    }

    private void EventOnly1TimeStart()
    {
        if (!_isCalCulatedStart)
        {
            _isCalCulatedStart = true;
            _comboSkill.SinglPreStart(_elapsedTime, _bothModel);
        }
    }

    private void EventOnly1TimeUpdate()
    {
        if (!_isCalCulatedUpdate)
        {
            _isCalCulatedUpdate = true;
            _comboSkill.SinglPreUpdate(_elapsedTime, _bothModel);
        }
    }

    private void EventOnly1TimeFinish()
    {
        if (!_isCalCulatedFinish)
        {
            _isCalCulatedFinish = true;
            _comboSkill.SinglPreFinish(_elapsedTime, _bothModel);
        }
    }


    private int Start(ICombo comboSkill , float elapsedTime , BothModel bothModel)
    {
       return  comboSkill.StartEvent(elapsedTime, bothModel);
    }

    private int Update(ICombo comboSkill, float elapsedTime, BothModel bothModel)
    {
        return comboSkill.UpdateEvent(elapsedTime, bothModel);
    }

    private int Final(ICombo comboSkill, float elapsedTime, BothModel bothModel)
    {
        return comboSkill.FinishEvent(elapsedTime, bothModel);
    }

    private int Fly(ICombo comboSkill, float elapsedTime, BothModel bothModel)
    {
        return comboSkill.FlyEvent(elapsedTime, bothModel);
    }

    private void SetCurrentAnim(string currentAnim)
    {
        _bothModel.SetCurrentAnimName(currentAnim);
    }





public void ExecutePlayerCombo(int targetObjId, AnimationCombo anim, int time, float dist, EffectSkillsmanager effectManager, int skill_id)
{
        _comboSkill = AllListImplCombo.GetClassCombo(skill_id);

        if (_comboSkill == null) return;

        _targetObjId = targetObjId;
        _animCombo = anim;
        _hitTime = time;
        _dist = dist;
        _effectManager = effectManager;

        _enabled = true;
        _complete = -1;
        _elapsedTime = 0;
        _starttime = Time.time * 1000;
        _bothModel = new BothModel(_hitTime, anim, dist , targetObjId);
        _bothModel.SetEffectManager(_effectManager);
        _isCalCulatedStart = false;
        _isCalCulatedUpdate = false;
        _isCalCulatedFinish = false;
    }


    void OnDestroy()
    {
        _instance = null;
    }
}
