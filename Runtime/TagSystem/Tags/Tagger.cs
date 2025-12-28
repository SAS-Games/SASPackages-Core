using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAS.Core.TagSystem
{
    [DisallowMultipleComponent()]
	public class Tagger : MonoBehaviour
	{
		[Serializable]
		public class InternalTag
		{
			[SerializeField] private Component m_Component;
			[SerializeField] private TagSystem.Tag m_Value;

			public Component Component => m_Component;

			public Tag Value { get => m_Value; set => m_Value = value; }

			public InternalTag(Component component, Tag val)
			{
				m_Component = component;
				m_Value = val;
			}
		}

		[SerializeField] private List<InternalTag> m_Tags = new List<InternalTag>();

		public IEnumerable<Component> Find<T>(Tag tag) where T : Component
		{
			return m_Tags.Where(item => item.Value == tag && item.Component.GetType() == typeof(T)).Select(item => item.Component);
		}

		public IEnumerable<Component> Find(Type type, Tag tag)
		{
			return m_Tags.Where(item => item.Value == tag && item.Component.GetType() == type).Select(item => item.Component);
		}

		public InternalTag Find(Component component)
		{
			return m_Tags.FirstOrDefault(tag => tag.Component == component);
		}

		public bool HasTag(Component component, Tag tag)
        {
			return m_Tags.Find(item => item.Component == component && item.Value == tag) != null;		
		}

		public string GetTag(Component component)
		{
			var tag = m_Tags.Find(ele => ele.Component == component);
			return tag?.Value.ToString();
		}

		public void AddTag(Component component, Tag tagValue = default)
		{
			var tag = m_Tags.Find(ele => ele.Component == component);
			if (tag == null || tag.Value != tagValue)
                m_Tags.Add(new InternalTag(component, default));
		}

		public void RemoveTag(Component component)
		{
			var tag = m_Tags.Find(ele => ele.Component == component);
			m_Tags.Remove(tag);
		}

		public void RemoveAllTags(Component component)
		{
			var tags = m_Tags.FindAll(ele => ele.Component == component);
			foreach (var tag in tags)
				m_Tags.Remove(tag);
		}

        public void LogAllTags(Component component)
        {
            var tags = m_Tags.FindAll(ele => ele.Component == component);
            foreach (var tag in tags)
               Debug.Log(tag.Value);
        }
    }
}