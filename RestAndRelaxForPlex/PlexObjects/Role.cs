using System;
using System.Collections.Generic;
using JimBobBennett.JimLib.Extensions;
using JimBobBennett.JimLib.Xml;

namespace JimBobBennett.RestAndRelaxForPlex.PlexObjects
{
    public class Role : IdTagObjectBase<Role>
    {
        [XmlNameMapping("Role")]
        public string RoleName { get; set; }

        public string Thumb { get; set; }

        public string ImdbId { get; set; }
        public bool HasImdbId { get { return !ImdbId.IsNullOrEmpty(); } }
        public Uri ImdbUrl { get { return ImdbId.IsNullOrEmpty() ? null : new Uri(string.Format(PlexResources.ImdbNameUrl, ImdbId)); } }
        public Uri ImdbSchemeUrl { get { return ImdbId.IsNullOrEmpty() ? null : new Uri(string.Format(PlexResources.ImdbNameSchemeUrl, ImdbId)); } }

        protected override bool OnUpdateFrom(IdTagObjectBase<Role> newValue, List<string> updatedPropertyNames)
        {
            var isUpdated = base.OnUpdateFrom(newValue, updatedPropertyNames);
            isUpdated = UpdateValue(() => RoleName, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => Thumb, newValue, updatedPropertyNames) | isUpdated;

            return isUpdated;
        }
    }
}
