using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace SAS.Core.TagSystem
{
    public static class TaggerExtensions
    {
        public static Component AddComponent(this Component component, Type type, Tag tag = default)
        {
            Tagger tagger = null;
            if (tag != default)
            {
                tagger = component.GetComponent<Tagger>();
                if (tagger == null)
                    tagger = component.gameObject.AddComponent<Tagger>();
            }

            var addedComponent = component.gameObject.AddComponent(type);

            if (tag != default)
                tagger.AddTag(addedComponent, tag);

            return addedComponent;
        }

        public static T AddComponent<T>(this Component component, Tag tag = default)
        {
            return (T)(object)component.AddComponent(typeof(T), tag); 
        }

        public static T GetComponent<T>(this Component component, Tag tag)
        {
            return (T)(object)component.GetComponent(typeof(T), tag);
        }

        public static Component GetComponent(this Component component, Type type, Tag tag)
        {
            return GetComponentByTag(component.GetComponents(type), tag);
        }

        public static Component[] GetComponents(this Component component, Type type, Tag tag)
        {
            return GetComponentsByTag(component.GetComponents(type), tag);
        }

        public static T GetComponentInChildren<T>(this Component component, Tag tag, bool includeInactive = false)
        {
            return (T)(object)component.GetComponentInChildren(typeof(T), tag, includeInactive);
        }

        public static Component GetComponentInChildren(this Component component, Type type, Tag tag, bool includeInactive = false)
        {
            return GetComponentByTag(component.GetComponentsInChildren(type, includeInactive), tag);
        }

        public static T GetComponentInParent<T>(this Component component, Tag tag, bool includeInactive = false)
        {
            return (T)(object)component.GetComponentsInParent(typeof(T), tag, includeInactive);
        }

        public static Component GetComponentInParent(this Component component, Type type, Tag tag, bool includeInactive = false)
        {
            return GetComponentByTag(component.GetComponentsInParent(type, includeInactive), tag);
        }

        public static T[] GetComponentsInParent<T>(this Component component, Tag tag, bool includeInactive = false)
        {
            List<T> result = new List<T>();
            var components = component.GetComponentsInParent(typeof(T), tag, includeInactive);
            foreach (var c in components)
                result.Add((T)(object)c);
            return result.ToArray();
        }

        public static Component[] GetComponentsInParent(this Component component, Type type, Tag tag, bool includeInactive = false)
        {
            return GetComponentsByTag(component.GetComponentsInParent(type, includeInactive), tag);
        }

        public static T[] GetComponentsInChildren<T>(this Component component, Tag tag, bool includeInactive = false)
        {
            List<T> result = new List<T>();
            var components = component.GetComponentsInChildren(typeof(T), tag, includeInactive);
            foreach (var c in components)
                result.Add((T)(object)c);
            return result.ToArray();
        }

        public static Component[] GetComponentsInChildren(this Component component, Type type, Tag tag, bool includeInactive = false)
        {
            return GetComponentsByTag(component.GetComponentsInChildren(type, includeInactive), tag);
        }

        private static T GetComponentByTag<T>(T[] components, Tag tag) where T : Component
        {
            if (tag == default)
                return components.FirstOrDefault();
            else
                return components.FirstOrDefault(component => HasTag(component, tag));
        }

        private static T[] GetComponentsByTag<T>(T[] components, Tag tag) where T : Component
        {
            if (tag == default)
                return components;

            List<T> result = new List<T>();
            foreach (T component in components)
            {
                if (HasTag(component, tag))
                    result.Add(component);
            }
            return result.ToArray();
        }

        private static bool HasTag(Component component, Tag tag)
        {
            var tagger = component.GetComponent<Tagger>();
            if (tagger != null)
                return tagger.HasTag(component, tag);
            else
                return false;
        }

        public static Tag GetTag(this Component component)
        {
            var c = component.GetComponent<Tagger>();
            if (c == null)
                return default;
            
            var taggerTag = c.Find(component);
            if (taggerTag == null)
                return default;
            
            return taggerTag.Value;
        }
    }
}
