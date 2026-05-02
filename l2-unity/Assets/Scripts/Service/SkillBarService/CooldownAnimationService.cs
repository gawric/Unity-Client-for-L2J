using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class CooldownAnimationService : MonoBehaviour
{
    public IEnumerator CooldownCoroutine(VisualElement _reuseElement , VisualElement _rechargeMaskBg,  float durationMs)
    {

        float duration = TimeUtils.ConvertMsToSec(durationMs);
        _reuseElement.style.display = DisplayStyle.Flex;

        int silzeCooltime = IconManager.Instance.GetSizeOtherCoolTime();

        float timePerImage = duration / silzeCooltime;
        float elapsed = 0f;


        for (int i = 1; i < silzeCooltime; i++)
        {
            var icon = IconManager.Instance.GetOtherIconByType(i, 0);
            if (icon == null) continue;
            _reuseElement.style.backgroundImage = icon;
            //_reuseElement.style.opacity = 0; ;

            float waitTime = i < silzeCooltime - 1 ? timePerImage : duration - elapsed;

            yield return new WaitForSeconds(waitTime);

            elapsed += waitTime;
        }


        float completionDuration = 1f; // 1 second total
        float fadeOutDuration = 0.6f; // 30% of the time
        float elapsedCompletion = 0f;

        SetupCompletionFrame( _reuseElement,  _rechargeMaskBg);

        while (elapsedCompletion < completionDuration)
        {
            float progress = elapsedCompletion / completionDuration;


            float maskOpacity = elapsedCompletion < fadeOutDuration ?
                1f * (1f - (elapsedCompletion / fadeOutDuration)) : 0f;


            float elementOpacity = 1f * (1f - progress);

            _reuseElement.style.opacity = elementOpacity;
            _rechargeMaskBg.style.opacity = maskOpacity;

            yield return null;
            elapsedCompletion += Time.deltaTime;
        }


        CleanupCompletionFrame( _reuseElement,  _rechargeMaskBg);

    }


    private void SetupCompletionFrame(VisualElement _reuseElement, VisualElement _rechargeMaskBg)
    {
        _rechargeMaskBg.style.opacity = 1;
        _rechargeMaskBg.style.display = DisplayStyle.Flex;
        _reuseElement.style.backgroundImage = IconManager.Instance.LoadTextureByName("Icon_dualcap");
        _reuseElement.style.backgroundColor = new Color(0f, 0f, 0f, 0.9f);
    }

    private void CleanupCompletionFrame(VisualElement _reuseElement, VisualElement _rechargeMaskBg)
    {
        // Reset reuse element
        _reuseElement.style.display = DisplayStyle.None;
        _reuseElement.style.backgroundColor = new Color(0f, 0f, 0f, 0.1f);
        _reuseElement.style.backgroundImage = null;
        _reuseElement.style.opacity = 1;

        // Reset mask background
        _rechargeMaskBg.style.display = DisplayStyle.None;
        _rechargeMaskBg.style.opacity = 1;
    }
}
