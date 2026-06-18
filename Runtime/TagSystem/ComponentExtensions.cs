using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAS.Core.TagSystem
{
    public static class ComponentExtensions
    {
        private static readonly Dictionary<Type, Func<Component, Type, Tag, Component>> _componentCreator = new Dictionary<Type, Func<Component, Type, Tag, Component>>
        {
            { typeof(InjectAttribute), (comp, type, tag) => comp.AddComponent(type, tag) },
        };

        private static readonly Dictionary<Type, Func<Component, Type, bool, Component>> _componentFetchers = new Dictionary<Type, Func<Component, Type, bool, Component>>
        {
            { typeof(FieldRequiresSelfAttribute), (comp, type, includeInactive) => comp.GetComponent(type) },
            { typeof(FieldRequiresChildAttribute), (comp, type, includeInactive) => comp.GetComponentInChildren(type, includeInactive) },
            { typeof(FieldRequiresParentAttribute), (comp, type, includeInactive) => comp.GetComponentInParent(type) },
            { typeof(FieldRequiresInSceneAttribute), (comp, type, includeInactive) => GetObjectWithComponentInScene(type,default, includeInactive) },
        };

        private static readonly Dictionary<Type, Func<Component, Type, Tag, bool, Component>> _componentWithTagFetchers = new Dictionary<Type, Func<Component, Type, Tag, bool, Component>>
        {
            { typeof(FieldRequiresSelfAttribute), (comp, type, tag, includeInactive) => comp.GetComponent(type, tag) },
            { typeof(FieldRequiresChildAttribute), (comp, type, tag, includeInactive) => comp.GetComponentInChildren(type, tag, includeInactive) },
            { typeof(FieldRequiresParentAttribute), (comp, type, tag, includeInactive) => comp.GetComponentInParent(type, tag, includeInactive) },
            { typeof(FieldRequiresInSceneAttribute), (comp, type, tag,includeInactive) => GetObjectWithComponentInScene(type, tag, includeInactive) },
        };

        private static readonly Dictionary<Type, Func<Component, Type, bool, Component[]>> _componentsFetchers = new Dictionary<Type, Func<Component, Type, bool, Component[]>>
        {
             { typeof(FieldRequiresSelfAttribute), (comp, type, includeInactive) => comp.GetComponents(type) },
             { typeof(FieldRequiresChildAttribute), (comp, type, includeInactive) => comp.GetComponentsInChildren(type, includeInactive) },
             { typeof(FieldRequiresParentAttribute), (comp, type, includeInactive) => comp.GetComponentsInParent(type, includeInactive) },
             { typeof(FieldRequiresInSceneAttribute), (comp, type, includeInactive) => GetObjectsWithComponentInScene(type,default, includeInactive) },
        };

        private static Dictionary<Type, Func<Component, Type, Tag, bool, Component[]>> _componentsWithTagFetchers = new Dictionary<Type, Func<Component, Type, Tag, bool, Component[]>>
        {
             { typeof(FieldRequiresSelfAttribute), (comp, type, tag, includeInactive) => comp.GetComponents(type, tag) },
             { typeof(FieldRequiresChildAttribute), (comp, type, tag, includeInactive) => comp.GetComponentsInChildren(type, tag, includeInactive) },
             { typeof(FieldRequiresParentAttribute), (comp, type, tag, includeInactive) => comp.GetComponentsInParent(type, tag, includeInactive) },
             { typeof(FieldRequiresInSceneAttribute), (comp, type, tag,includeInactive) => GetObjectsWithComponentInScene(type, tag, includeInactive) },
        };

        internal static Dictionary<string, IContextBinder> _cachedContext = new Dictionary<string, IContextBinder>();

        public static void Initialize(this Component component, object instance = null)
        {
            instance = instance ?? component;
            var allFields = GetAllFields(instance);
            var binders = CollectBinders(component.gameObject);

            foreach (var field in allFields)
            {
                var requirement = field.GetCustomAttribute<BaseRequiresAttribute>(false);
                if (requirement != null)
                {
                    if (requirement is BaseRequiresComponent)
                    {
                        var componentRequirement = requirement as BaseRequiresComponent;
                        if (field.FieldType.IsArray)
                        {
                            var elementType = field.FieldType.GetElementType();
                            var dependencies = default(Component[]);
                            if (requirement.tag == default)
                                dependencies = _componentsFetchers[requirement.GetType()](component, elementType, componentRequirement.includeInactive);
                            else
                                dependencies = _componentsWithTagFetchers[requirement.GetType()](component, elementType, requirement.tag, componentRequirement.includeInactive);

                            field.SetValue(instance, ConvertArray(dependencies, elementType));
                        }
                        else
                        {
                            var dependency = default(Component);
                            if (requirement.tag == default)
                                dependency = _componentFetchers[requirement.GetType()](component, field.FieldType, componentRequirement.includeInactive);
                            else
                                dependency = _componentWithTagFetchers[requirement.GetType()](component, field.FieldType, requirement.tag, componentRequirement.includeInactive);
                            field.SetValue(instance, dependency);
                        }
                    }
                    else if (requirement is InjectAttribute)
                    {
                        var modelRequirement = requirement as InjectAttribute;
                        foreach (var binder in binders)
                        {
                            if (Inject(binder, instance, field, modelRequirement))
                                break;
                        }
                    }
                }
            }
        }

        public static void InjectFieldBindings(this Component component, object instance = null)
        {
            instance = instance ?? component;
            var allFields = GetAllFields(instance);
            var binders = CollectBinders(component.gameObject);

            foreach (var field in allFields)
            {
                var requirement = field.GetCustomAttribute<InjectAttribute>(false);
                if (requirement == null)
                    continue;
                bool injected = false;
                foreach (var binder in binders)
                {
                    if (Inject(binder, instance, field, requirement))
                    {
                            injected = true;
                            break;
                    }
                }
                if (!injected)
                {
                    if (field.DeclaringType != null)
                    {
                        var message = $"Failed to inject `{field.FieldType.Name}` into `{field.DeclaringType.FullName}.{field.Name}` on `{component.name}`.\n" +
                                      $"No matching binding found in Object, Scene, or Project-level contexts.";
                        Debug.LogError(message);
                    }
                }
            }
        }

        private static IEnumerable<FieldInfo> GetAllFields(this object instance)
        {
            var instanceType = instance.GetType();
            var baseType = instanceType.BaseType;

            FieldInfo[] fields = instanceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var enumerable = fields.AsEnumerable();

            while (baseType != null)
            {
                var baseFields = baseType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                enumerable = enumerable.Concat(baseFields);
                baseType = baseType.BaseType;
            }

            return fields.AsEnumerable();
        }

        private static Array ConvertArray<T>(T[] elements, Type castType)
        {
            var array = Array.CreateInstance(castType, elements.Length);
            for (var i = 0; i < elements.Length; ++i)
                array.SetValue(elements[i], i);

            return array;
        }

        private static List<IContextBinder> CollectBinders(GameObject gameObject)
        {
            var result = new List<IContextBinder>();

            var objectBinder = gameObject.GetComponentInParent<IContextBinder>();
            if (objectBinder is { BinderScope: Scope.ObjectLevel })
                result.Add(objectBinder);

            if (TryGetSceneLevelContext(gameObject, out var sceneLevelContext))
                result.Add(sceneLevelContext);
           
            if (_cachedContext.TryGetValue("DontDestroyOnLoad", out var crossContext))
                result.Add(crossContext);
            
            return result;
        }

        private static bool TryGetSceneLevelContext(GameObject gameObject, out IContextBinder context)
        {
            context = null;
             if (!_cachedContext.TryGetValue(gameObject.scene.name, out context) && gameObject.scene.isLoaded)
            {
                var scene = gameObject.scene;
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObject in rootObjects)
                {
                    if (rootObject.TryGetComponent(out context))
                    {
                        if (context.BinderScope == Scope.SceneLevel)
                        {
                            _cachedContext[scene.name] = context;
                            return true;
                        }
                    }
                }
                return false;
            }
             return context != null;
        }

        private static bool Inject(IContextBinder context, object instance, FieldInfo field, InjectAttribute requirement)
        {
            if (context == null) 
                return false;
            
            if (requirement.optional)
            {
                if (context.TryGet(field.FieldType, out var obj))
                {
                    field.SetValue(instance, obj);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                var value = context.GetOrCreate(field.FieldType, requirement.tag);
                if (value != null)
                {
                    field.SetValue(instance, value);
                    return true;
                }
                return false;
            }
        }

        private static Component GetObjectWithComponentInScene(Type type, Tag tag, bool includeInactive = false)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var result = root.transform.GetComponentInChildren(type, tag, includeInactive);
                if (result != null)
                    return result;

            }
            return null;
        }

        private static Component[] GetObjectsWithComponentInScene(Type type, Tag tag, bool includeInactive = false)
        {
            List<Component> result = new List<Component>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var components = root.transform.GetComponentsInChildren(type, tag, includeInactive);
                result.AddRange(components);
            }
            return result.ToArray();
        }
    }
}