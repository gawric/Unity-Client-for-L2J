using UnityEngine;
using UnityEngine.UIElements;

public class CreateScroller
{
    protected float _scrollStepSize = 32f;
    private bool _autoscroll = false;
    public void Start(VisualElement _tabContainer)
    {
        if (_tabContainer != null)
        {

            ScrollView scrollView = _tabContainer.Q<ScrollView>("ScrollView");

            if (scrollView != null)
            {
                Scroller scroller = scrollView.verticalScroller;
                RegisterPlayerScrollEvent(scrollView, scroller);
            }
        }
    }

    public void RegisterPlayerScrollEvent(ScrollView scrollView, Scroller scroller)
    {
        if (scroller != null)
        {
            var highBtn = scroller.Q<RepeatButton>("unity-high-button");
            var lowBtn = scroller.Q<RepeatButton>("unity-low-button");
            var dragger = scroller.Q<VisualElement>("unity-drag-container");

            highBtn.RegisterCallback<MouseUpEvent>(evt => {
                AdjustScrollValue(scrollView, scroller, 1);
                VerifyScrollValue(scroller);
            });
            lowBtn.RegisterCallback<MouseUpEvent>(evt => {
                AdjustScrollValue(scrollView, scroller, -1);
                VerifyScrollValue(scroller);
            });

            highBtn.AddManipulator(new ButtonClickSoundManipulator(highBtn));
            lowBtn.AddManipulator(new ButtonClickSoundManipulator(lowBtn));

            dragger.RegisterCallback<MouseUpEvent>(evt => {
                VerifyScrollValue(scroller);
            });

            dragger.RegisterCallback<WheelEvent>(evt => {
                VerifyScrollValue(scroller);
            });

            // _windowEle.RegisterCallback<GeometryChangedEvent>(evt => {
            //     OnGeometryChanged();
            // });

            scrollView.RegisterCallback<WheelEvent>(evt => {
                int direction = evt.delta.y > 0 ? 1 : -1;
                AdjustScrollValue(scrollView, scroller, direction);
                VerifyScrollValue(scroller);
                evt.StopPropagation();
            });
        }

    }

    protected void AdjustScrollValue(ScrollView scrollView, Scroller scroller, int direction)
    {
        if (scrollView == null || scroller == null) return;

        float contentHeight = scrollView.contentContainer.worldBound.height;
        float viewportHeight = scrollView.worldBound.height;

        if (contentHeight <= viewportHeight) return; // No need to scroll if content fits in viewport

        float scrollRange = contentHeight - viewportHeight;
        float stepSize = _scrollStepSize / scrollRange;
        float newValue = direction * (scroller.value + stepSize) * scroller.highValue;
        scroller.value = Mathf.Clamp(newValue, 0, scroller.highValue);
    }

    private void VerifyScrollValue(Scroller scroller)
    {
        if (scroller.highValue > 0 && scroller.value == scroller.highValue || scroller.highValue == 0 && scroller.value == 0)
        {
            _autoscroll = true;
        }
        else
        {
            _autoscroll = false;
        }
    }
}
