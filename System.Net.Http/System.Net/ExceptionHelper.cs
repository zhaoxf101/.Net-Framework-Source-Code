using System;
using System.Text.RegularExpressions;

namespace System.Net
{
	internal static class ExceptionHelper
	{
		internal static readonly WebPermission WebPermissionUnrestricted = new WebPermission(NetworkAccess.Connect, new Regex(".*"));
	}
}
