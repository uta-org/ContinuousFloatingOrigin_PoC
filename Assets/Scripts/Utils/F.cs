using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace CFO.Utils
{
    public static class F
    {
        public static void AddRange<T>(this ObservableCollection<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }

        public static T GetAttribute<T>(this Type t, bool inherit = false)
            where T : Attribute
        {
            return t.GetCustomAttributes(inherit)
                .FirstOrDefault(a => a.GetType() == typeof(T)) as T;
        }

        public static T GetAttribute<T>(this MemberInfo m, bool inherit = false)
            where T : Attribute
        {
            return m.GetCustomAttributes(inherit)
                .FirstOrDefault(a => a.GetType() == typeof(T)) as T;
        }

        public static void SetValue<T>(this MemberInfo memberInfo, object forObject, T value)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    ((FieldInfo)memberInfo).SetValue(forObject, value);
                    break;

                case MemberTypes.Property:
                    ((PropertyInfo)memberInfo).SetValue(forObject, value);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public static Type GetMemberUnderlyingType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;

                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;

                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;

                default:
                    throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", "member");
            }
        }

        public static object InvokeWithOutParam<T>(this MethodInfo method, object instance, out T outResult, params object[] args)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            var parameters = new object[args.Length + 1];
            for (int i = 0; i < args.Length; i++)
            {
                if (i >= args.Length) break;
                parameters[i] = args[i];
            }

            var result = method.Invoke(instance, parameters);
            bool blResult = (bool)result;
            if (blResult) outResult = (T)parameters[1];
            else outResult = default;
            return result;
        }

        public static MethodInfo FindMethod(this IEnumerable<MethodInfo> methods, string name, params Type[] types)
        {
            return methods
                .Where(m => m.Name.Equals(name))
                .Select(m => new
                {
                    Method = m,
                    Parameters = m.GetParameters()
                })
                .FirstOrDefault(m =>
                    types.Length == m.Parameters.Length && m.Parameters.Select(p => p.ParameterType).SequenceEqual(types))?.Method;
        }
    }
}