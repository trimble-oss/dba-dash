namespace DBADashGUI
{

    /// <summary>
    /// Interface for navigation operations.  e.g. Navigate back.  
    /// Controls implement this common navigation interface which is used to support global navigation buttons.
    /// 
    /// </summary>
    public interface INavigation
    {

        /// <summary>
        /// Return true if navigation back is possible
        /// </summary>
        public bool CanNavigateBack { get; }

        // Consider for future?
        // public bool CanNavigateForward { get; }


        /// <summary>
        /// Perform navigate back operation
        /// </summary>
        /// <returns>True if navigation back is performed.  False if navigate back is not possible</returns>
        public bool NavigateBack();

        // Consider for future?
        //public bool NavigateForward();

    }
}
