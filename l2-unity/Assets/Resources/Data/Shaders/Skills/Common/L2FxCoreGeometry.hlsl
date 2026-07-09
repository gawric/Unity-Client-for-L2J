#ifndef L2FX_CORE_GEOMETRY_INCLUDED
#define L2FX_CORE_GEOMETRY_INCLUDED

// Эталон перевода UU в метры по стандарту UE2.5 (1м = 52.5 UU)
static const float L2_UU_TO_METERS = 1.0 / 52.5;

// Вычисление локального размера меша в метрах Unity без скрытых умножений/делений
float L2Fx_GetFinalVertexSizeMeters(float sizeUU, float worldCalibK)
{
    float k = worldCalibK > 0.0 ? worldCalibK : 1.0;
    return (sizeUU * L2_UU_TO_METERS) * k;
}

#endif
