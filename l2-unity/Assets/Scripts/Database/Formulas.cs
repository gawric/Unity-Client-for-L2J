using System;
using UnityEngine;

public class Formulas 
{

    public static readonly int MAX_STAT_VALUE = 100;


    private static readonly double[] STR_COMPUTE = new double[]
      {
        1.036,
        34.845
      };
    private static readonly double[] INT_COMPUTE = new double[]
    {
        1.020,
        31.375
    };
    private static readonly double[] DEX_COMPUTE = new double[]
    {
        1.009,
        19.360
    };
    private static readonly double[] WIT_COMPUTE = new double[]
    {
        1.050,
        20.000
    };
    private static readonly double[] CON_COMPUTE = new double[]
    {
        1.030,
        27.632
    };
    private static readonly double[] MEN_COMPUTE = new double[]
    {
        1.010,
        -0.060
    };

    public static readonly double[] WIT_BONUS = new double[MAX_STAT_VALUE];
    public static readonly double[] MEN_BONUS = new double[MAX_STAT_VALUE];
    public static readonly double[] INT_BONUS = new double[MAX_STAT_VALUE];
    public static readonly double[] STR_BONUS = new double[MAX_STAT_VALUE];
    public static readonly double[] DEX_BONUS = new double[MAX_STAT_VALUE];
    public static readonly double[] CON_BONUS = new double[MAX_STAT_VALUE];

    public static readonly double[] BASE_EVASION_ACCURACY = new double[MAX_STAT_VALUE];

    protected static readonly double[] SQRT_MEN_BONUS = new double[MAX_STAT_VALUE];
    protected static readonly double[] SQRT_CON_BONUS = new double[MAX_STAT_VALUE];

    public static void Init()
	{
		for (int i = 0; i<STR_BONUS.Length; i++)
			STR_BONUS[i] = Math.Floor(Math.Pow(STR_COMPUTE[0], i - STR_COMPUTE[1]) * 100 + .5d) / 100;
		for (int i = 0; i<INT_BONUS.Length; i++)
			INT_BONUS[i] = Math.Floor(Math.Pow(INT_COMPUTE[0], i - INT_COMPUTE[1]) * 100 + .5d) / 100;
		for (int i = 0; i<DEX_BONUS.Length; i++)
			DEX_BONUS[i] = Math.Floor(Math.Pow(DEX_COMPUTE[0], i - DEX_COMPUTE[1]) * 100 + .5d) / 100;
		for (int i = 0; i<WIT_BONUS.Length; i++)
			WIT_BONUS[i] = Math.Floor(Math.Pow(WIT_COMPUTE[0], i - WIT_COMPUTE[1]) * 100 + .5d) / 100;
		for (int i = 0; i<CON_BONUS.Length; i++)
			CON_BONUS[i] = Math.Floor(Math.Pow(CON_COMPUTE[0], i - CON_COMPUTE[1]) * 100 + .5d) / 100;
		for (int i = 0; i<MEN_BONUS.Length; i++)
			MEN_BONUS[i] = Math.Floor(Math.Pow(MEN_COMPUTE[0], i - MEN_COMPUTE[1]) * 100 + .5d) / 100;
		
		for (int i = 0; i<BASE_EVASION_ACCURACY.Length; i++)
			BASE_EVASION_ACCURACY[i] = Math.Sqrt(i)* 6;
		
		// Precompute square root values
		for (int i = 0; i<SQRT_CON_BONUS.Length; i++)
			SQRT_CON_BONUS[i] = Math.Sqrt(CON_BONUS[i]);
		for (int i = 0; i<SQRT_MEN_BONUS.Length; i++)
			SQRT_MEN_BONUS[i] = Math.Sqrt(MEN_BONUS[i]);
	}

    public static float calc(int dex ,  float value)
    {
       // double bonus = Formulas.DEX_BONUS[dex];
        //float result = (float) value * bonus;
        return 0f;
    }
}
