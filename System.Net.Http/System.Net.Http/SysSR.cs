using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace System.Net.Http
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0"), DebuggerNonUserCode, CompilerGenerated]
	internal class SysSR
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(SysSR.resourceMan, null))
				{
					ResourceManager resourceManager = new ResourceManager("System.Net.Http.SysSR", typeof(SysSR).Assembly);
					SysSR.resourceMan = resourceManager;
				}
				return SysSR.resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return SysSR.resourceCulture;
			}
			set
			{
				SysSR.resourceCulture = value;
			}
		}

		internal static string net_log_exception
		{
			get
			{
				return SysSR.ResourceManager.GetString("net_log_exception", SysSR.resourceCulture);
			}
		}

		internal SysSR()
		{
		}
	}
}
