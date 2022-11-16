namespace DBADashGUI
{
    internal interface ISetContext
    {

        /// <summary>
        /// Sets the context of the current control and refreshes data
        /// </summary>
        public void SetContext(DBADashContext context);

    }
}
