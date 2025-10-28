using UnityEngine;

public class EffectSkillsmanager : MonoBehaviour
{
    private GameObject _skillsEffect;

    private Transform _currentTransfromEffect;
    private static EffectSkillsmanager _instance;
    private float _worldTimeCombo = 0;
    public float _hitTime = 0;
    public Transform _rootBodyEffect;
    public static EffectSkillsmanager Instance { get { return _instance; } }


    void Start()
    {
        _skillsEffect = GameObject.Find("Skills");
        
    }

    public void UpdateWorldTime(float worldTimeCombo)
    {
        _worldTimeCombo = worldTimeCombo;
    }

    public double GetPercentCompleteTime()
    {
        return  (_worldTimeCombo / _hitTime) * 100;
    }

    public double GetPieceCompleteTimeMs(double percent)
    {
        return  (percent / 100) * _hitTime;
    }

    public float HitTime()
    {
        return _hitTime;
    }

    public float GetWorldTime()
    {
        return _worldTimeCombo;
    }
    public void ShowEffect(int skillId , Vector3 footerPosition, Vector3 bodyPosition , float hitTime , Entity monster)
    {
        _hitTime = hitTime;

        _currentTransfromEffect = _skillsEffect.transform.Find(skillId.ToString());
        GameObject go = _currentTransfromEffect.gameObject;
        go.gameObject.SetActive(true);

        _currentTransfromEffect.position = GetPosition—ollision(footerPosition, S_1177.MOVE_FROM_ORIGINAL_POSITION_FOOTER);
        

        string[] effectsFooter = S_1177.NAME_FOOTER_EFFECT;
        string[] effectsBody = S_1177.NAME_BODY_EFFECT;

        var _rootFooterEffect = go.transform.Find(S_1177.NAME_FOOTER_OBJECT);
        _rootBodyEffect = go.transform.Find(S_1177.NAME_BODY_OBJECT);

        //_rootBodyEffect.position = bodyPosition;
        //_rootBodyEffect.position = GetPosition—ollision(bodyPosition, S_1177.MOVE_FROM_ORIGINAL_POSITION_FOOTER);
        _rootBodyEffect.position = bodyPosition;
        //_rootBodyEffect.LookAt(monster.transform);
        foreach (string i in effectsFooter)
        {
            ShowEffectFooter(go, _rootFooterEffect, i, S_1177.NAME_FOOTER_OBJECT);
        }

        foreach (string i in effectsBody)
        {
            ShowEffectBody(go, _rootBodyEffect, i, S_1177.NAME_BODY_OBJECT);
        }

    }

    public Transform GetBodyTransform()
    {
        return _rootBodyEffect;
    }

    private Vector3 GetPosition—ollision(Vector3 transformPlayer , float pos)
    {
        return new Vector3(transformPlayer.x, transformPlayer.y + pos, transformPlayer.z);
    }

    public void ShowEffectFooter(GameObject go  , Transform _footerEffect, string effectFooterName , string effectFooterObject)
    {
        if (effectFooterName.Equals("windblowin00"))
        {

            var _effectFooter = _footerEffect.transform.Find(effectFooterName);
            var _effectFooter01 = _footerEffect.transform.Find("windblowin01");
            FlyWindStrike fws = _effectFooter.gameObject.GetComponent<FlyWindStrike>();
            FlyWindStrike01 fws01 = _effectFooter01.gameObject.GetComponent<FlyWindStrike01>();
            fws.SetFlS01(fws01);
            fws.SetShow();
        }
        else if (effectFooterName.Equals("auraplane00"))
        {
            var _effectFooter = _footerEffect.transform.Find(effectFooterName);
            AuraWindStrike aws = _effectFooter.gameObject.GetComponent<AuraWindStrike>();
            aws.SetShow();
        }

    }

    public void ShowEffectBody(GameObject go, Transform _bodyEffect ,  string effectBodyName, string effectBodyObject)
    {
        if (effectBodyName.Equals("sphere_shader"))
        {
            var _efectSpere = _bodyEffect.transform.Find(effectBodyName);
            BodyWindStrike bws01 = _efectSpere.gameObject.GetComponent<BodyWindStrike>();
            bws01.SetShow(false);
        }
        else if (effectBodyName.Equals("glowvfx"))
        {
            var _efect01 = _bodyEffect.transform.Find(effectBodyName);
            VfxWindStrike bws01 = _efect01.gameObject.GetComponent<VfxWindStrike>();
            bws01.SetShow();
        }
        else if (effectBodyName.Equals("bodywindblowin01"))
        {
            var _efect01 = _bodyEffect.transform.Find(effectBodyName);
            BodyWindStrike bws01 = _efect01.gameObject.GetComponent<BodyWindStrike>();
            bws01.SetShow(true);
        }
        else if (effectBodyName.Equals("bodywindblowin02"))
        {
            var _efect02 = _bodyEffect.transform.Find(effectBodyName);
            BodyWindStrike bws02 = _efect02.gameObject.GetComponent<BodyWindStrike>();
            bws02.SetShow(true);
        }
    }
    public async void StartFlyOrActive(int skillId , int targetObjId , float speed, float timeToReachTarget)
    {
        _currentTransfromEffect = _skillsEffect.transform.Find(skillId.ToString());
        GameObject go = _currentTransfromEffect.gameObject;
        Entity targetEntity = await World.Instance.GetEntityNoLock(targetObjId);
        var _rootBodyEffect = go.transform.Find(S_1177.NAME_BODY_OBJECT);
        FlyObjectToTarget foToTarget = _rootBodyEffect.gameObject.GetComponent<FlyObjectToTarget>();
        //Debug.Log("StartFlyOrActive: OltTime " + oldTime + " dist " + dist);
        //foToTarget.StartFly(targetEntity.transform.position , 10 , oldTime);
        foToTarget.StartFly(targetEntity, speed, timeToReachTarget);
    }
    public void HideEffect(float elapsedTime , int skillId, float timeToTravel)
    {
        _currentTransfromEffect = _skillsEffect.transform.Find(skillId.ToString());
        GameObject go = _currentTransfromEffect.gameObject;
        float remainingTime = RemainingTime(elapsedTime, _hitTime);
        var _rootBodyEffect = go.transform.Find(S_1177.NAME_BODY_OBJECT);

        string[] effectsFooter = S_1177.NAME_FOOTER_EFFECT;
        string[] effectsBody = S_1177.NAME_BODY_EFFECT;

        for (int i = 0; i < effectsFooter.Length; i++)
        {
            HideEffectFooter(go, effectsFooter[i], S_1177.NAME_FOOTER_OBJECT);
        }

        for (int i = 0; i < effectsBody.Length; i++)
        {
            HideEffectBody( _rootBodyEffect , effectsBody[i] ,  remainingTime);
        }

    }

    private float RemainingTime(float elapseedTime , float hittime)
    {
        return hittime - elapseedTime;
    }

    public void HideEffectFooter(GameObject go, string effectFooterName, string effectFooterObject)
    {
        if (effectFooterName.Equals("windblowin00"))
        {
            var _footerEffect = go.transform.Find(effectFooterObject);
            var _effectFooter = _footerEffect.transform.Find(effectFooterName);
            FlyWindStrike fws = _effectFooter.gameObject.GetComponent<FlyWindStrike>();
            fws.SetHide();
        }
        else if (effectFooterName.Equals("auraplane00"))
        {
            var _footerEffect = go.transform.Find(effectFooterObject);
            var _effectFooter = _footerEffect.transform.Find(effectFooterName);
            AuraWindStrike fws = _effectFooter.gameObject.GetComponent<AuraWindStrike>();
            fws.SetHide();
        }

    }

    public void HideEffectBody( Transform _bodyEffect ,  string effectBodyName , float remainingTime)
    {
        if (effectBodyName.Equals("sphere_shader"))
        {
            var _efectSpere = _bodyEffect.transform.Find(effectBodyName);
            BodyWindStrike bws01 = _efectSpere.gameObject.GetComponent<BodyWindStrike>();
            float rem = remainingTime / 1000f ;
            float rem1 = rem + 0.7f;
            bws01.SetElapsedHide(rem1);
            bws01.SetHide();
        }
        else if (effectBodyName.Equals("glowvfx"))
        {
            var _efect01 = _bodyEffect.transform.Find(effectBodyName);
            VfxWindStrike bws01 = _efect01.gameObject.GetComponent<VfxWindStrike>();
            bws01.SetHide();
        }
        else if (effectBodyName.Equals("bodywindblowin01"))
        {
            var _efect01 = _bodyEffect.transform.Find(effectBodyName);
            BodyWindStrike bws01 = _efect01.gameObject.GetComponent<BodyWindStrike>();
            bws01.SetHide();
        }
        else if (effectBodyName.Equals("bodywindblowin02"))
        {
            var _efect02 = _bodyEffect.transform.Find(effectBodyName);
            BodyWindStrike bws02 = _efect02.gameObject.GetComponent<BodyWindStrike>();
            bws02.SetHide();
        }

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


    void Update()
    {
        
    }


    void OnDestroy()
    {
        _instance = null;
    }
}
