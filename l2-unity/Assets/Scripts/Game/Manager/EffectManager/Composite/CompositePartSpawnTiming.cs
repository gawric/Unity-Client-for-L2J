public enum CompositePartSpawnTiming
{
    Immediate = 0,
    OnServerShootTime = 1,   // castData.serverTimeToShoot (HitTime - FlightTime)
    OnFlightTimeElapsed = 2, // castData.FlightTime
    OnHitTime = 3            // castData.HitTime
}

