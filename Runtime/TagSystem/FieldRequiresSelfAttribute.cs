using System;

namespace SAS.Core.TagSystem
{
	[AttributeUsage(AttributeTargets.Field)]
	public class FieldRequiresSelfAttribute : BaseRequiresComponent
	{
		public FieldRequiresSelfAttribute(int tag = 0)
		{
			this.tag = tag;
		}
	}
}
