using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Csla;
using Csla.Validation;
using CslaEx;

namespace CslaEx.Validation
{
    public static class CommonRulesEx
    {

        #region Strings

        /// <summary>
        /// Rule ensuring a string value contains one or more
        /// characters.
        /// </summary>
        /// <param name="target">Object containing the data to validate</param>
        /// <param name="e">Arguments parameter specifying the name of the string
        /// property to validate</param>
        /// <returns><see langword="false" /> if the rule is broken</returns>
        /// <remarks>
        /// This implementation uses late binding, and will only work
        /// against string property values.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public static bool UCaseStringRequired(object target, RuleArgs e)
        {
            string value = (string)Utilities.CallByName(
              target, e.PropertyName, CallType.Get);
            string pattern = "[A-Z]+";

            if (!Regex.IsMatch(value, pattern))
            {
                e.Description = string.Format(Properties.Resources.UCaseStringRequiredRule, e.PropertyName);
                return false;
            }
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public static bool LCaseStringRequired(object target, RuleArgs e)
        {
            string value = (string)Utilities.CallByName(
              target, e.PropertyName, CallType.Get);
            string pattern = "[a-z]+";

            if (!Regex.IsMatch(value, pattern))
            {
                e.Description = string.Format(Properties.Resources.LCaseStringRequiredRule, e.PropertyName);
                return false;
            }
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public static bool NumberStringRequired(object target, RuleArgs e)
        {
            string value = (string)Utilities.CallByName(
              target, e.PropertyName, CallType.Get);
            string pattern = "[0-9]+";

            if (!Regex.IsMatch(value, pattern))
            {
                e.Description = string.Format(Properties.Resources.NumberStringRequiredRule, e.PropertyName);
                return false;
            }
            return true;
        }

        #endregion
    }
}
