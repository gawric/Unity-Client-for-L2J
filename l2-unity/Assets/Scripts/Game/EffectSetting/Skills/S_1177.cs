using UnityEngine;

public class S_1177
{
    public static readonly float MOVE_FROM_ORIGINAL_POSITION_FOOTER = 0.2f;
    public static readonly float MOVE_FROM_ORIGINAL_POSITION_BODY = 0.15f;
    public static readonly string[] NAME_FOOTER_EFFECT = new string[2] { "windblowin00" , "auraplane00" };
    public static readonly string[] NAME_BODY_EFFECT = new string[4] { "glowvfx" , "sphere_shader", "bodywindblowin01" , "bodywindblowin02" };
    public static readonly string NAME_FOOTER_OBJECT = "Footer";
    public static readonly string NAME_BODY_OBJECT = "Body";
    public static readonly int startSize = 110;
    public static float defaultSizeXYZ = 23;
    public static float defaultBodySizeXYZ = 10;
    public static float defaulAuratSizeXYZ = 0.1f;

    private static double startTime1 = 17;
    private static double scale1 = 90;

    private static double startTime2 = 50;
    private static double scale2 = 70;

    //public static readonly float hideTime = 2f;
    //public static readonly float showTime = 1f;

    public static Scale[] GetScale()
    {
        return new Scale[2] { new Scale(startTime1, scale1), new Scale(startTime2, scale2) };
    }

    public static Scale[] GetScaleBody()
    {
        return new Scale[2] { new Scale(20f, 70), new Scale(50, 90) };
    }


    public class Scale
    {
        private float _startTime1;
        private float _scale1;

        public Scale(double startTime1, double scale1)
        {
            _startTime1 =(float) startTime1;
            _scale1 = (float)scale1;
        }

        public float TimeProcent { get => _startTime1; }
        public float ScaleSizeProcent { get => _scale1; }
    }
}
