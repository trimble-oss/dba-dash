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
        public static readonly Color Warning = YellowDark;
        public static readonly Color Fail = RedDark;
        public static readonly Color Success = GreenDark;
        public static readonly Color NotApplicable = DashColors.GrayLight;

        //Neutral Progression
        public static readonly Color GrayLight = Color.FromArgb(241, 241, 246);

        //Other
        public static readonly Color GridViewBackground = Color.White;
        public static readonly Color LinkColor = TrimbleBlueDark;
        public static readonly Color ProgressBarFrom = Color.White;
        public static readonly Color ProgressBarTo = Color.FromArgb(103, 176, 228);


    }
}
