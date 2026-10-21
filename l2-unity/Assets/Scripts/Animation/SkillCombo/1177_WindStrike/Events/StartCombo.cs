using System;
using UnityEngine;

public class StartCombo : AbstractAnimCombo
{

  
    public void CalcCompression(float elapsedTime, BothModel bothModel, float launchTimeSex)
    {
        CalculateTimeCompression(elapsedTime, bothModel, launchTimeSex);
    }
    public int Event(float elapsedTime, BothModel bothModel , float launchTime)
    {
        string currentAnimName = bothModel.GetCurrentAnimName();
        if (!PlayerAnimationController.Instance.IsFinishAnimation(currentAnimName))
        {
            return -1;
        }
        else
        {
            //Debug.Log("Elapsed time to end start event " + _elapsedTime);
            // Debug.Log("Start TIME Stop " + elapsedTime);
            return 0;
        }
    }

    private void CalculateTimeCompression(float elapsedTime, BothModel bothModel, float launchTimeSec)
    {
        //fire 600ms
        float totalAnimationTime = GetTotalAnimationTime(bothModel.GetAllCombo().GetAnimCycle());
        var compressionFactor1 = totalAnimationTime / ConvertHitTimeToSec(bothModel.GetHitTime());
        // bothModel.SetCompressionByIndex(0, compressionFactor1);

        float randomSpeed = CalculateSpeed(bothModel.GetDist());
        bothModel.SetTimeToTravel(bothModel.GetDist() / randomSpeed);
        bothModel.SetSpeedFly(randomSpeed);
        //Может пригодится
        // Новые длительности анимаций
        float T1_new = 2.250f / compressionFactor1;
        float T2_new = 1.875f / compressionFactor1;
        float T3_new = 0.125f / compressionFactor1;
        float T_ALL = T1_new + T2_new + T3_new;
        float T_ALL2 = T_ALL - bothModel.GetTimeToTravel();
        //0.6f - время выстрела (2250 время на 1 ролик + 600 это время выстрела во 2 ролике)
        var d = launchTimeSec / 1.875f;
        var newEventTime = d * T2_new;
        float sizeAnim2 = bothModel.GetTimeToTravel() + newEventTime;

        if (sizeAnim2 > 1.0f)
        {
            if (sizeAnim2 < T2_new)
            {
                // Debug.Log("time travel anim2 " + sizeAnim2);
                float plusAnim1 = Math.Abs(sizeAnim2 - T2_new);

                float newAnim2 = sizeAnim2;
                float newAnim1 = T1_new + plusAnim1;

                // Debug.Log("time travel anim1 " + newAnim1);
                float anim1_2 = newAnim2 + newAnim1;
                //float anim3 = _hitTimeSec - anim1_2;
                float anim3 = ConvertHitTimeToSec(bothModel.GetHitTime()) - anim1_2;


                var compressionFactor3 = anim3 / T3_new;
                var compressionFactor2 = newAnim2 / T2_new;
                compressionFactor1 = newAnim1 / T1_new;
                // revers = compressionFactor1;

                bothModel.SetCompressionByIndex(2, compressionFactor3);
                bothModel.SetCompressionByIndex(1, compressionFactor2);
                bothModel.SetCompressionByIndex(0, compressionFactor1);

                //Debug.Log(" Общее время ролика " + all );

            }
            else
            {
                bothModel.SetCompressionAll(compressionFactor1);
            }
        }
        else
        {
            //0,6 start launch replace 1800

            // float totaltime2 = 0.6f / 4f;
            bothModel.SetCompressionAll(compressionFactor1);
            // compressionFactor3 = compressionFactor1;
            //compressionFactor2 = compressionFactor1;
            //compressionFactor1 = compressionFactor1;

            //var compressionFactorOr = 3f / 4f;
            // float T2_new_0 = 1.875f / totaltime2;
            //≈ 0.656
            //float t1_1 = T1_new + 0.6f;
            //float t2_1 = T2_new - 0.6f;
            //float t1_new_0 = T1_new + T2_new;
            //float launchTime = T1_new + 0.6f;//Launch Time
            // float offset = _hitTimeSec - launchTime;
            // float new_time_ofset = _hitTimeSec + offset;
            // float speed = t1_new_0 / new_time_ofset;
            //compressionFactor1 = t1_1 / T1_new;
            // Debug.Log("");
        }

        // compressionFactor = T_ALL / T_ALL2;
        //revers = compressionFactor;
        //Debug.Log("Время полета " + timeToTravel);
        //////////default anim to === hittime 
        /// does not take into account attack range
        //float originalAnimMs = AnimLeghtTable.Instance.GetLeghtMs(_anim);
        //float newAnimMs = originalAnimMs + _shiftHitTimMs;
        ////float originalAnimSec = originalAnimMs / 1000f;
        //float newAnimSec = newAnimMs / 1000f;
        //float revers = originalAnimSec / newAnimSec;
        //////////default anim to === hittime 

        float compression = bothModel.GetCompressionByIndex(0);
        PlayerAnimationController.Instance.SetCastSpeed(bothModel.GetCompressionByIndex(0));
        PlayerAnimationController.Instance.SetBool(bothModel.GetCurrentAnimName(), true);
        // Debug.Log("Start TIM FIRST " + elapsedTime);
    }
}
