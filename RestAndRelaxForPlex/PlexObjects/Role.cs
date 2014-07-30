using System.Collections.Generic;
using JimBobBennett.JimLib.Xml;

namespace JimBobBennett.RestAndRelaxForPlex.PlexObjects
{
    public class Role : IdTagObjectBase<Role>
    {
        [XmlNameMapping("Role")]
        public string RoleName { get; set; }

        public string Thumb { get; set; }

        protected override bool OnUpdateFrom(IdTagObjectBase<Role> newValue, List<string> updatedPropertyNames)
        {
            var isUpdated = base.OnUpdateFrom(newValue, updatedPropertyNames);
            isUpdated = UpdateValue(() => RoleName, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => Thumb, newValue, updatedPropertyNames) | isUpdated;

            return isUpdated;
        }
    }
}
