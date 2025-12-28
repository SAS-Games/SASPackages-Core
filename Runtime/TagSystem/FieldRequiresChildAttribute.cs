using System;

namespace SAS.Core.TagSystem
{
	[AttributeUsage(AttributeTargets.Field)]
	public class FieldRequiresChildAttribute : BaseRequiresComponent
	{
		public FieldRequiresChildAttribute(int tag = 0, bool includeInactive = false)
		{
			this.includeInactive = includeInactive;
			this.tag = tag;
		}
	}
}
