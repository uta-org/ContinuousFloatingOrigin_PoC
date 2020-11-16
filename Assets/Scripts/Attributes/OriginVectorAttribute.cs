using System;
using System.Linq;
using System.Reflection;
using CFO.Utils;
using UnityEngine;

namespace CFO.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    [AttributeTypeUsage(typeof(Vector3))]
    public class OriginVectorAttribute : Attribute
    {
        public bool IsAllowedType(MemberInfo info)
        {
            var usage = GetType().GetAttribute<AttributeTypeUsageAttribute>();
            if (usage == null) throw new InvalidOperationException($"{nameof(OriginVectorAttribute)} must include {nameof(AttributeTypeUsageAttribute)} attribute.");
            return usage.Types.Contains(info.GetMemberUnderlyingType());
        }
    }
}