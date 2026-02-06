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
        // 1. Фиксируем скорость начала и конца (пусть будут родными)
        SpeedMid = 1.0f;
        SpeedShot = 1.0f;

        // 2. Считаем, сколько времени займут Mid и Shot при скорости 1.0
        float fixedWorkTime = durMid + durShotToEvent;

        // 3. Всё оставшееся время отдаем фазе END
        float timeLeftForEnd = serverTimeToShoot - fixedWorkTime;

        if (timeLeftForEnd > 0)
        {
            // Если время есть, растягиваем END (скорость будет меньше 1.0)
            SpeedEnd = durEnd / timeLeftForEnd;
        }
        else
        {
            // Если времени не хватает даже на замах и выстрел, 
            // ускоряем всё пропорционально (старая логика)
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