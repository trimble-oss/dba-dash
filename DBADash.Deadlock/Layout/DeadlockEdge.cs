namespace DBADash.Deadlock.Layout
{
    /// <summary>What the arrow between a process and a resource means.</summary>
    public enum DeadlockEdgeKind
    {
        /// <summary>
        /// The process holds the resource.  Drawn resource -&gt; process, matching the convention SSMS
        /// uses, so the arrow points at whoever is holding things up.
        /// </summary>
        Owner,

        /// <summary>The process is blocked waiting for the resource.  Drawn process -&gt; resource.</summary>
        Waiter
    }

    /// <summary>
    /// A routed connection between two nodes.  <see cref="Start"/> and <see cref="End"/> are already
    /// clipped to the node borders, so a renderer draws a straight line between them and puts an
    /// arrowhead at <see cref="End"/> without repeating the geometry.
    /// </summary>
    public sealed class DeadlockEdge
    {
        public DeadlockEdgeKind Kind { get; internal set; }

        public DeadlockNode From { get; internal set; } = null!;

        public DeadlockNode To { get; internal set; } = null!;

        public LayoutPoint Start { get; internal set; }

        public LayoutPoint End { get; internal set; }

        /// <summary>Midpoint of the routed line, where a renderer would place <see cref="Label"/>.</summary>
        public LayoutPoint LabelAnchor { get; internal set; }

        /// <summary>
        /// The lock mode, e.g. "Owner: X" or "Requested: U".  Empty when the graph gave no mode,
        /// which is normal for parallelism resources such as exchangeEvent.
        /// </summary>
        public string Label { get; internal set; } = string.Empty;

        /// <summary>True when this edge forms part of the deadlock cycle.</summary>
        public bool IsInCycle { get; internal set; }
    }
}
