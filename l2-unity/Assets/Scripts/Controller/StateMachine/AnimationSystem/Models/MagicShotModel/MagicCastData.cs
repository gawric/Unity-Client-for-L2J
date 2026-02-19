using UnityEngine;

public class MagicCastData
{
    public float StartTime;
    public float HitTime;
    public float FlightTime;

    // Индивидуальные скорости для каждой фазы
    public float SpeedMid = 1.0f;
    public float SpeedEnd = 1.0f;
    public float SpeedShot = 1.0f;
    public float shotEventTime;
    public float serverTimeToShoot;
    public void Setup(float serverHitMs, float flyMs, float[] clipsDurations, float shotEventTime)
    {
        StartTime = Time.time;
        HitTime = serverHitMs / 1000f;
        FlightTime = flyMs / 1000f;

        serverTimeToShoot = Mathf.Max(0.01f, HitTime - FlightTime);

        float durMid = clipsDurations[0];
        float durEnd = clipsDurations[1];
        float durShotToEvent = (shotEventTime > 0) ? shotEventTime : clipsDurations[2];
        this.shotEventTime = shotEventTime;

        SpeedMid = 1.0f;
        SpeedShot = 1.0f;

  
        float fixedWorkTime = durMid + durShotToEvent;

 
        float timeLeftForEnd = serverTimeToShoot - fixedWorkTime;

        if (timeLeftForEnd > 0)
        {

            SpeedEnd = durEnd / timeLeftForEnd;
        }
        else
        {

            float totalWork = durMid + durEnd + durShotToEvent;
            float globalSpeed = totalWork / serverTimeToShoot;
            SpeedMid = SpeedEnd = SpeedShot = globalSpeed;
        }
    }

    public float GetShotTimeNormalize()
    {
        float fadeStartProgress = serverTimeToShoot / HitTime;
        float shaderFadeStart = Mathf.Max(0, (serverTimeToShoot / HitTime));
        return shaderFadeStart;
    }
}