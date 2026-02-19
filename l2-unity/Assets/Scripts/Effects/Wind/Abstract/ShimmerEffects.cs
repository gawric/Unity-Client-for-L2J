using UnityEngine;
using UnityEngine.VFX;
using static S_1177;

public class ShimmerEffects
{
    private Material fadeOutMaterial;
    private VisualEffect fadeOutVfx;
    //private float alphaHide;
    public void SetMaterial(Material fadeOutMaterial)
    {
        this.fadeOutMaterial = fadeOutMaterial;
    }
    public void SetVfx(VisualEffect fadeOutVfx)
    {
        this.fadeOutVfx = fadeOutVfx;
    }

    public void HideElement(float elapsedTime , float sec , ref float alphaHide , float startIndex)
    {
        if(fadeOutMaterial != null)
        {
            alphaHide = Mathf.Clamp01(startIndex - (elapsedTime / sec));
            fadeOutMaterial.SetFloat("_Alpha", alphaHide);
        }

        if(fadeOutVfx != null)
        {
            alphaHide = Mathf.Clamp01(startIndex - (elapsedTime / sec));
            fadeOutVfx.SetFloat("_Alpha", alphaHide);
        }

    }

    public void ShowElement(float elapsedTime, float sec , ref float alphaHide)
    {
        if (fadeOutMaterial != null)
        {
            alphaHide = Mathf.Clamp01(elapsedTime / sec);
            fadeOutMaterial.SetFloat("_Alpha", alphaHide);
        }
  

        if (fadeOutVfx != null)
        {
            alphaHide = Mathf.Clamp01(elapsedTime / sec);
            fadeOutVfx.SetFloat("_Alpha", alphaHide);
        }
    }

    public void StartScalePosition(Transform transform , float scaleXYZ)
    {
        transform.localScale = new Vector3(scaleXYZ, scaleXYZ, scaleXYZ);
    }

    public void UpdateScale(double percentTime,  float elapsedTime , Scale scale , float defaultXYZ  , Transform targetTransform  , float plusTimeElseNeedMs)
    {
        
           float time1177_2 = scale.TimeProcent;
           float procent_end_scale = scale.ScaleSizeProcent;

            if (percentTime >= time1177_2 & percentTime <= 100)
            {
                //convert second (/ 150)
                //var duration = (float)EffectSkillsmanager.Instance.GetPieceCompleteTimeMs(procent_end_scale) / 150;

                float endSize = ConvertProcentToSize(procent_end_scale, defaultXYZ);

                float ms = EffectSkillsmanager.Instance.HitTime() + plusTimeElseNeedMs;
                
                float sec = (ms / 1000 );


                float t = Mathf.Clamp01(elapsedTime / sec); // 
                Vector3 targetScale = new Vector3(endSize, endSize, endSize);


                var z = Vector3.Lerp(new Vector3(defaultXYZ , defaultXYZ , defaultXYZ), targetScale, t);
                targetTransform.localScale = z;

            }
    }

    public void UpdateScaleStraightAway(double percentTime, float elapsedTime, Scale scale, float defaultXYZ, Transform targetTransform, float plusTimeElseNeedMs)
    {

        float time1177_2 = scale.TimeProcent;
        float procent_end_scale = scale.ScaleSizeProcent;

        //if (percentTime >= time1177_2 & percentTime <= 100)
        //{


            float endSize = ConvertProcentToSize(procent_end_scale, defaultXYZ);



            float sec = (plusTimeElseNeedMs / 1000);


            float t = Mathf.Clamp01(elapsedTime / sec); // 
            Vector3 targetScale = new Vector3(endSize, endSize, endSize);

            Debug.Log("Scale Last01 target scale " + targetScale + " defaultScale " + defaultXYZ);
            var z = Vector3.Lerp(new Vector3(defaultXYZ, defaultXYZ, defaultXYZ), targetScale, t);
            targetTransform.localScale = z;

        //}
    }

    public float ConvertProcentToSize(double percent, float allSize)
    {
        return (float)(percent / 100) * allSize;
    }

}


