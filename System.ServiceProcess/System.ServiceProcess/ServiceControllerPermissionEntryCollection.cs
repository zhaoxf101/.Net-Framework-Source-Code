using System;
using System.Collections;
using System.Security.Permissions;

namespace System.ServiceProcess
{
	[Serializable]
	public class ServiceControllerPermissionEntryCollection : CollectionBase
	{
		private ServiceControllerPermission owner;

		public ServiceControllerPermissionEntry this[int index]
		{
			get
			{
				return (ServiceControllerPermissionEntry)base.List[index];
			}
			set
			{
				base.List[index] = value;
			}
		}

		internal ServiceControllerPermissionEntryCollection(ServiceControllerPermission owner, ResourcePermissionBaseEntry[] entries)
		{
			this.owner = owner;
			for (int i = 0; i < entries.Length; i++)
			{
				base.InnerList.Add(new ServiceControllerPermissionEntry(entries[i]));
			}
		}

		public int Add(ServiceControllerPermissionEntry value)
		{
			return base.List.Add(value);
		}

		public void AddRange(ServiceControllerPermissionEntry[] value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			for (int i = 0; i < value.Length; i++)
			{
				this.Add(value[i]);
			}
		}

		public void AddRange(ServiceControllerPermissionEntryCollection value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			int count = value.Count;
			for (int i = 0; i < count; i++)
			{
				this.Add(value[i]);
			}
		}

		public bool Contains(ServiceControllerPermissionEntry value)
		{
			return base.List.Contains(value);
		}

		public void CopyTo(ServiceControllerPermissionEntry[] array, int index)
		{
			base.List.CopyTo(array, index);
		}

		public int IndexOf(ServiceControllerPermissionEntry value)
		{
			return base.List.IndexOf(value);
		}

		public void Insert(int index, ServiceControllerPermissionEntry value)
		{
			base.List.Insert(index, value);
		}

		public void Remove(ServiceControllerPermissionEntry value)
		{
			base.List.Remove(value);
		}

		protected override void OnClear()
		{
			this.owner.Clear();
		}

		protected override void OnInsert(int index, object value)
		{
			this.owner.AddPermissionAccess((ServiceControllerPermissionEntry)value);
		}

		protected override void OnRemove(int index, object value)
		{
			this.owner.RemovePermissionAccess((ServiceControllerPermissionEntry)value);
		}

		protected override void OnSet(int index, object oldValue, object newValue)
		{
			this.owner.RemovePermissionAccess((ServiceControllerPermissionEntry)oldValue);
			this.owner.AddPermissionAccess((ServiceControllerPermissionEntry)newValue);
		}
	}
}
