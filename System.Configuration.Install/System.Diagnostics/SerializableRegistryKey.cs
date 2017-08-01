using Microsoft.Win32;
using System;
using System.Runtime.Serialization;

namespace System.Diagnostics
{
	[Serializable]
	internal class SerializableRegistryKey
	{
		public string[] ValueNames;

		public object[] Values;

		[OptionalField(VersionAdded = 2)]
		public RegistryValueKind[] ValueKinds;

		public string[] KeyNames;

		public SerializableRegistryKey[] Keys;

		public SerializableRegistryKey(RegistryKey keyToSave)
		{
			this.CopyFromRegistry(keyToSave);
		}

		public void CopyFromRegistry(RegistryKey keyToSave)
		{
			if (keyToSave == null)
			{
				throw new ArgumentNullException("keyToSave");
			}
			this.ValueNames = keyToSave.GetValueNames();
			if (this.ValueNames == null)
			{
				this.ValueNames = new string[0];
			}
			this.Values = new object[this.ValueNames.Length];
			this.ValueKinds = new RegistryValueKind[this.ValueNames.Length];
			for (int i = 0; i < this.ValueNames.Length; i++)
			{
				this.Values[i] = keyToSave.GetValue(this.ValueNames[i], null, RegistryValueOptions.DoNotExpandEnvironmentNames);
				this.ValueKinds[i] = keyToSave.GetValueKind(this.ValueNames[i]);
			}
			this.KeyNames = keyToSave.GetSubKeyNames();
			if (this.KeyNames == null)
			{
				this.KeyNames = new string[0];
			}
			this.Keys = new SerializableRegistryKey[this.KeyNames.Length];
			for (int j = 0; j < this.KeyNames.Length; j++)
			{
				this.Keys[j] = new SerializableRegistryKey(keyToSave.OpenSubKey(this.KeyNames[j]));
			}
		}

		public void CopyToRegistry(RegistryKey baseKey)
		{
			if (baseKey == null)
			{
				throw new ArgumentNullException("baseKey");
			}
			if (this.Values != null)
			{
				for (int i = 0; i < this.Values.Length; i++)
				{
					if (this.ValueKinds != null)
					{
						baseKey.SetValue(this.ValueNames[i], this.Values[i], this.ValueKinds[i]);
					}
					else
					{
						baseKey.SetValue(this.ValueNames[i], this.Values[i]);
					}
				}
			}
			if (this.Keys != null)
			{
				for (int j = 0; j < this.Keys.Length; j++)
				{
					RegistryKey baseKey2 = baseKey.CreateSubKey(this.KeyNames[j]);
					this.Keys[j].CopyToRegistry(baseKey2);
				}
			}
		}
	}
}
