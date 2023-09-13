namespace DBADashSharedGUI
{
    //https://modus.trimble.com/foundations/color-palette/
    public static class DashColors
    {
        // Primary
        public static readonly Color TrimbleBlueDark = Color.FromArgb(0, 79, 131);

        public static readonly Color TrimbleBlue = Color.FromArgb(0, 99, 163);
        public static readonly Color TrimbleYellow = Color.FromArgb(251, 173, 38);
        public static readonly Color TrimbleGray = Color.FromArgb(37, 42, 46);

        // Blue Progression
        public static readonly Color BlueDark = Color.FromArgb(14, 65, 108);

        public static readonly Color Blue = Color.FromArgb(0, 99, 163);
        public static readonly Color BlueLight = Color.FromArgb(33, 124, 187);
        public static readonly Color BluePale = Color.FromArgb(220, 237, 249);

        // Yellow Progression
        public static readonly Color YellowDark = Color.FromArgb(228, 147, 37); // Warning

        public static readonly Color Yellow = Color.FromArgb(251, 173, 38);
        public static readonly Color YellowLight = Color.FromArgb(254, 193, 87);
        public static readonly Color YellowPale = Color.FromArgb(255, 245, 228);

        // Red Progression
        public static readonly Color RedDark = Color.FromArgb(171, 31, 38); // Danger/Error

        public static readonly Color Red = Color.FromArgb(218, 33, 44);
        public static readonly Color RedLight = Color.FromArgb(232, 99, 99);
        public static readonly Color RedPale = Color.FromArgb(251, 212, 215);

        // Green Progression
        public static readonly Color GreenDark = Color.FromArgb(0, 102, 56); // Success

        public static readonly Color Green = Color.FromArgb(30, 138, 68);
        public static readonly Color GreenLight = Color.FromArgb(78, 166, 70);
        public static readonly Color GreenPale = Color.FromArgb(224, 236, 207);

        // Success/Warn/Fail
        public static readonly Color Warning = Color.FromArgb(228, 147, 37);

        public static readonly Color Fail = Color.FromArgb(171, 31, 38);
        public static readonly Color Success = Color.FromArgb(0, 102, 56);
        public static readonly Color SuccessLightBackground = Color.FromArgb(255, 0, 210, 47);
        public static readonly Color SuccessDarkBackground = Color.FromArgb(255, 0, 254, 0);
        public static readonly Color AvoidanceZone = Color.FromArgb(255, 223, 78, 178);
        public static readonly Color Information = Color.FromArgb(255, 1, 154, 235);

        public static Color NotApplicable => DashColors.Gray0;

        //Neutral Progression
        public static readonly Color GrayLight = Color.FromArgb(241, 241, 246);

        public static readonly Color Gray0 = Color.FromArgb(255, 224, 225, 233);
        public static readonly Color Gray1 = Color.FromArgb(255, 203, 205, 214);
        public static readonly Color Gray2 = Color.FromArgb(255, 183, 185, 195);
        public static readonly Color Gray3 = Color.FromArgb(255, 163, 166, 177);
        public static readonly Color Gray4 = Color.FromArgb(255, 144, 147, 159);
        public static readonly Color Gray5 = Color.FromArgb(255, 125, 128, 141);
        public static readonly Color Gray6 = Color.FromArgb(255, 106, 110, 121);
        public static readonly Color Gray7 = Color.FromArgb(255, 88, 92, 101);
        public static readonly Color Gray8 = Color.FromArgb(255, 70, 75, 82);
        public static readonly Color Gray9 = Color.FromArgb(255, 53, 58, 64);
        public static readonly Color Gray10 = Color.FromArgb(255, 23, 28, 30);
        public static readonly Color White = Color.FromArgb(255, 255, 255, 255);

        //Other
        public static readonly Color GridViewBackground = Color.White;

        public static readonly Color LinkColor = TrimbleBlueDark;
        public static readonly Color ProgressBarFrom = Color.White;
        public static readonly Color ProgressBarTo = Color.FromArgb(103, 176, 228);

        public static Color DatabaseLevelTitleColor => TrimbleBlue;
    }
}