namespace WPFZoomPanel.Helpers
{
    internal static class DoubleHelpers
    {
        #region Public Methods

        public static double ToRealNumber(this double value, double defaultValue = 0) => (double.IsInfinity(value) || double.IsNaN(value)) ? defaultValue : value;

        #endregion Public Methods
    }
}
