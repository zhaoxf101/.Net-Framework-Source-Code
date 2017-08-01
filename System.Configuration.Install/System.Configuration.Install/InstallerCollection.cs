using System;
using System.Collections;
using System.ComponentModel;

namespace System.Configuration.Install
{
	public class InstallerCollection : CollectionBase
	{
		private Installer owner;

		public Installer this[int index]
		{
			get
			{
				return (Installer)base.List[index];
			}
			set
			{
				base.List[index] = value;
			}
		}

		internal InstallerCollection(Installer owner)
		{
			this.owner = owner;
		}

		public int Add(Installer value)
		{
			return base.List.Add(value);
		}

		public void AddRange(InstallerCollection value)
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

		public void AddRange(Installer[] value)
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

		public bool Contains(Installer value)
		{
			return base.List.Contains(value);
		}

		public void CopyTo(Installer[] array, int index)
		{
			base.List.CopyTo(array, index);
		}

		public int IndexOf(Installer value)
		{
			return base.List.IndexOf(value);
		}

		public void Insert(int index, Installer value)
		{
			base.List.Insert(index, value);
		}

		public void Remove(Installer value)
		{
			base.List.Remove(value);
		}

		protected override void OnInsert(int index, object value)
		{
			if (value == this.owner)
			{
				throw new ArgumentException(Res.GetString("CantAddSelf"));
			}
			bool arg_23_0 = CompModSwitches.InstallerDesign.TraceVerbose;
			((Installer)value).parent = this.owner;
		}

		protected override void OnRemove(int index, object value)
		{
			bool arg_0A_0 = CompModSwitches.InstallerDesign.TraceVerbose;
			((Installer)value).parent = null;
		}

		protected override void OnSet(int index, object oldValue, object newValue)
		{
			if (newValue == this.owner)
			{
				throw new ArgumentException(Res.GetString("CantAddSelf"));
			}
			bool arg_23_0 = CompModSwitches.InstallerDesign.TraceVerbose;
			((Installer)oldValue).parent = null;
			((Installer)newValue).parent = this.owner;
		}
	}
}
