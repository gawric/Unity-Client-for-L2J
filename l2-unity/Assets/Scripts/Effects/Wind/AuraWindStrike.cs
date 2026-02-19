using UnityEngine;
using static S_1177;


public class AuraWindStrike : MonoBehaviour
{
    private Material _fadeOutMaterial;
    private ShimmerEffects _shimmerEffects;
    private float _alphaHide = 0;
    private float _elapsedTime = 0f;
    private bool _isHide = false;
    private bool _isShow = false;
    void Awake()
    {
        GameObject targetObject = this.gameObject;
        Renderer renderer = targetObject.GetComponent<Renderer>();

        if(_shimmerEffects == null) _shimmerEffects = new ShimmerEffects();

        if (renderer != null)
        {
            _fadeOutMaterial = renderer.material;
            _fadeOutMaterial.SetFloat("_Alpha", 0);
        }

        _shimmerEffects.SetMaterial(_fadeOutMaterial);
    }

    // Update is called once per frame
    void Update()
    {
        _elapsedTime += Time.deltaTime;
        UpdateScale(_elapsedTime , _isHide);
        if (_isShow)
        {
            if (_alphaHide >= 0.3f) _isShow = false;
            _shimmerEffects.ShowElement(_elapsedTime, 3f , ref _alphaHide);
        }

        if (_isHide)
        {
            
            if (_alphaHide <= 0) _isHide = false;
            _shimmerEffects.HideElement(_elapsedTime, 4f ,  ref _alphaHide , 0.5f);
            
        }
    }

    private void UpdateScale(float elapsedTime , bool isHide)
    {
        if(isHide == true)
        {
            _shimmerEffects.UpdateScale(EffectSkillsmanager.Instance.GetPercentCompleteTime(), elapsedTime , new Scale(50 , 30) , S_1177.defaulAuratSizeXYZ  ,  transform , 1000);
        }
        
    }

    public void SetShow()
    {
        DisableCollision();
        _shimmerEffects.StartScalePosition(transform, S_1177.defaulAuratSizeXYZ);
        _alphaHide = 0;
        _elapsedTime = 0;
        _isShow = true;

    }

    public void SetHide()
    {
        _isHide = true;
        _isShow = false;
        _alphaHide = 0.3f;
        _elapsedTime = 0;
    }

    private void DisableCollision()
    {
        Vector3 vector = GetPositionРЎollision(transform.position, GetGround());
        transform.localPosition = vector;
    }

    private float ConvertToNegativeValue(float number)
    {
        return -Mathf.Abs(number); 
    }

    private Vector3 GetPositionРЎollision(Vector3 transformPlayer, float pos)
    {
        return new Vector3(0,  pos, 0);
    }

    public float GetGround()
    {
        float ground0 =  ConvertToNegativeValue(S_1177.MOVE_FROM_ORIGINAL_POSITION_FOOTER);
        float groundAura = (float)ground0 + 0.01f;
        return groundAura;
    }
}
