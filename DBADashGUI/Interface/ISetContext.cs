namespace DBADashGUI
{
    public interface ISetContext
    {

        /// <summary>
        /// Sets the context of the current control and refreshes data
        /// </summary>
        public void SetContext(DBADashContext _context);

        /// <summary>
        /// Sets the context of the current control.  With <paramref name="forceRefresh"/>, data is reloaded even if the
        /// control already has this context - used when following a link, where the user expects current data.  Controls
        /// that skip reloading for an unchanged context should implement this; the default just calls SetContext.
        /// </summary>
        public void SetContext(DBADashContext _context, bool forceRefresh) => SetContext(_context);

    }
}
