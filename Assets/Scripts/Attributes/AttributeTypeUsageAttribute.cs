using System;

namespace CFO.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AttributeTypeUsageAttribute : Attribute
    {
        public Type[] Types { get; }

        private AttributeTypeUsageAttribute()
        {
        }

        public AttributeTypeUsageAttribute(Type type)
        {
            Types = new[] { type };
        }

        public AttributeTypeUsageAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}