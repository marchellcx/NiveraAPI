using System.Reflection;

namespace NiveraAPI.Extensions
{
    /// <summary>
    /// Extensions targeting reflection members.
    /// </summary>
    public static class MemberExtensions
    {
        /// <summary>
        /// Gets the full name of a member.
        /// </summary>
        public static string GetMemberName(this MemberInfo member, bool includeDeclaringType = true,
            char separator = '.')
        {
            if (includeDeclaringType && member.DeclaringType != null)
                return $"{member.DeclaringType.FullName}{separator}{member.Name}";

            return member.Name;
        }

        /// <summary>
        /// Checks if a member has an attribute.
        /// </summary>
        public static bool HasAttribute<T>(this MemberInfo member, bool inherit = false) where T : Attribute
            => member.GetCustomAttribute<T>(inherit) != null;

        /// <summary>
        /// Checks if a member has an attribute.
        /// </summary>
        public static bool HasAttribute<T>(this MemberInfo member, out T attribute) where T : Attribute
            => (attribute = member.GetCustomAttribute<T>()) != null;

        /// <summary>
        /// Checks if a member has an attribute.
        /// </summary>
        public static bool HasAttribute<T>(this MemberInfo member, bool inherit, out T attribute) where T : Attribute
            => (attribute = member.GetCustomAttribute<T>(inherit)) != null;
        
        /// <summary>
        /// Attempts to retrieve a custom attribute of a specified type from the provided member.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the custom attribute to be retrieved.
        /// </typeparam>
        /// <param name="member">
        /// The member from which the attribute is to be retrieved (e.g., a class, method, or property).
        /// </param>
        /// <param name="attributeValue">
        /// When this method returns, contains the retrieved attribute of type <typeparamref name="T"/> if found; otherwise, the default value for the type.
        /// </param>
        /// <returns>
        /// True if the attribute of the specified type is found on the member; otherwise, false.
        /// </returns>
        public static bool TryGetAttribute<T>(this MemberInfo member, out T attributeValue)
        {
            var customAttributes = member.GetCustomAttributes();
		
            foreach (var item in customAttributes)
            {
                if (item.Is(out attributeValue))
                {
                    return true;
                }
            }

            attributeValue = default!;
            return false;
        }
    }
}