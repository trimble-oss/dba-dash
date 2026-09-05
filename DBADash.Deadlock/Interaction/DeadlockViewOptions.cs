namespace DBADash.Deadlock.Interaction
{
    /// <summary>Limits and step sizes for <see cref="DeadlockViewController"/>.</summary>
    public sealed class DeadlockViewOptions
    {
        public double MinZoom { get; set; } = 0.1;

        public double MaxZoom { get; set; } = 8.0;

        /// <summary>Multiplier applied per zoom-in step; its reciprocal is used to zoom out.</summary>
        public double ZoomStep { get; set; } = 1.2;

        /// <summary>Upper bound on the scale zoom-to-fit may use, so a small graph fills the
        /// window without being blown up unreasonably large.</summary>
        public double MaxFitZoom { get; set; } = 1.0;

        /// <summary>Statement text on a tooltip is collapsed to one line and truncated to this length.</summary>
        public int MaxTooltipStatementLength { get; set; } = DeadlockTooltipBuilder.DefaultMaxStatementLength;

        /// <summary>
        /// Re-fit the graph when the viewport changes size, so resizing the window keeps the whole
        /// deadlock in view.  Only applies until the user zooms or pans themselves - after that the
        /// view is theirs and resizing leaves it alone, until they ask to fit again.
        /// </summary>
        public bool RefitOnViewportChange { get; set; } = true;

        /// <summary>
        /// Draw the statement preview as a link even when nothing is subscribed to
        /// <see cref="DeadlockViewController.StatementActivated"/>.  Normally the preview is plain
        /// text and only becomes a clickable link once a consumer subscribes; set this to force the
        /// link appearance regardless.
        /// </summary>
        public bool ShowStatementLinkAlways { get; set; }
    }
}
