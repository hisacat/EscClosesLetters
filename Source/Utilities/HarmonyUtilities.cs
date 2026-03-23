#pragma warning disable IDE0130
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace HisaCat.Utilities
{
    public static class HarmonyUtilities
    {
        public static MethodInfo GetRootVirtualMethodOrThrow(Type rootType, string methodName,
            Type[] parameters = null, Type[] generics = null)
        {
            // Get the method from the root type.
            var method = AccessTools.Method(rootType, methodName, parameters, generics) ??
                throw new InvalidOperationException($"Method '{methodName}' was not found on type '{rootType.FullName}'.");

            // Check that the method is virtual.
            if (method.IsVirtual == false)
                throw new InvalidOperationException($"Method '{rootType.FullName}.{methodName}' is not virtual.");

            // Check that the root of the virtual chain is the specified root type.
            var baseDef = method.GetBaseDefinition();
            if (baseDef.DeclaringType != rootType)
            {
                throw new InvalidOperationException(
                    $"Type '{rootType.FullName}' is not the root declaring type of the virtual method '{methodName}'. " +
                    $"The root is '{baseDef.DeclaringType?.FullName ?? "null"}'."
                );
            }

            return method;
        }

        public static IEnumerable<MethodBase> GetOverriddenMethods(Type rootType, string methodName, bool includeRootType)
        {
            var rootMethod = AccessTools.Method(rootType, methodName) ??
                throw new InvalidOperationException($"Method '{methodName}' was not found on type '{rootType.FullName}'.");

            if (rootMethod.IsVirtual == false)
                throw new InvalidOperationException($"Method '{methodName}' on type '{rootType.FullName}' is not virtual.");

            // Get the root virtual method in the inheritance chain.
            var rootVirtualChainMethod = rootMethod.GetBaseDefinition();

            if (includeRootType)
            {
                if (IsVirtualMethodDeclaredOnType(rootType, rootMethod, rootVirtualChainMethod) == false)
                    throw new InvalidOperationException($"Type '{rootType.FullName}' does not declare its own implementation of the virtual method '{methodName}'.");

                yield return rootMethod;
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    if (type == rootType) continue;
                    if (rootType.IsAssignableFrom(type) == false) continue;
                    if (type.IsAbstract) continue;

                    // Try to get the method from this type.
                    var method = AccessTools.Method(type, methodName);
                    // var method = type.GetMethod(methodName,
                    //     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    // );

                    // Skip types that do not declare a method with this name.
                    if (method == null) continue;

                    if (IsVirtualMethodDeclaredOnType(type, method, rootVirtualChainMethod) == false) continue;

                    yield return method;
                }
            }
        }

        public static bool IsVirtualMethodDeclaredOnType(Type type, MethodInfo method, MethodBase rootVirtualChainMethod)
        {
            // If the method is not directly declared/overridden by this type, ignore it.
            if (method.DeclaringType != type) return false;

            // Ensure the method belongs to the same virtual chain as the root method.
            // This filters out unrelated methods (e.g. methods hiding the base with 'new').
            if (method.GetBaseDefinition() != rootVirtualChainMethod) return false;

            return true;
        }
    }
}
#pragma warning restore IDE0130
