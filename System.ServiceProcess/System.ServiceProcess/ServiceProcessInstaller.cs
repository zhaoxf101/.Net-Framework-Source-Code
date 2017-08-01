using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Runtime.InteropServices;
using System.ServiceProcess.Design;
using System.Text;

namespace System.ServiceProcess
{
	public class ServiceProcessInstaller : ComponentInstaller
	{
		private ServiceAccount serviceAccount = ServiceAccount.User;

		private bool haveLoginInfo;

		private string password;

		private string username;

		private static bool helpPrinted;

		public override string HelpText
		{
			get
			{
				if (ServiceProcessInstaller.helpPrinted)
				{
					return base.HelpText;
				}
				ServiceProcessInstaller.helpPrinted = true;
				return Res.GetString("HelpText") + "\r\n" + base.HelpText;
			}
		}

		[Browsable(false)]
		public string Password
		{
			get
			{
				if (!this.haveLoginInfo)
				{
					this.GetLoginInfo();
				}
				return this.password;
			}
			set
			{
				this.haveLoginInfo = false;
				this.password = value;
			}
		}

		[DefaultValue(ServiceAccount.User), ServiceProcessDescription("ServiceProcessInstallerAccount")]
		public ServiceAccount Account
		{
			get
			{
				if (!this.haveLoginInfo)
				{
					this.GetLoginInfo();
				}
				return this.serviceAccount;
			}
			set
			{
				this.haveLoginInfo = false;
				this.serviceAccount = value;
			}
		}

		[Browsable(false), TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
		public string Username
		{
			get
			{
				if (!this.haveLoginInfo)
				{
					this.GetLoginInfo();
				}
				return this.username;
			}
			set
			{
				this.haveLoginInfo = false;
				this.username = value;
			}
		}

		private static bool AccountHasRight(IntPtr policyHandle, byte[] accountSid, string rightName)
		{
			IntPtr intPtr = (IntPtr)0;
			int num = 0;
			int num2 = NativeMethods.LsaEnumerateAccountRights(policyHandle, accountSid, out intPtr, out num);
			if (num2 == -1073741772)
			{
				return false;
			}
			if (num2 != 0)
			{
				throw new Win32Exception(SafeNativeMethods.LsaNtStatusToWinError(num2));
			}
			bool result = false;
			try
			{
				IntPtr intPtr2 = intPtr;
				for (int i = 0; i < num; i++)
				{
					NativeMethods.LSA_UNICODE_STRING_withPointer lSA_UNICODE_STRING_withPointer = new NativeMethods.LSA_UNICODE_STRING_withPointer();
					Marshal.PtrToStructure(intPtr2, lSA_UNICODE_STRING_withPointer);
					char[] array = new char[(int)(lSA_UNICODE_STRING_withPointer.length / 2)];
					Marshal.Copy(lSA_UNICODE_STRING_withPointer.pwstr, array, 0, array.Length);
					string strA = new string(array, 0, array.Length);
					if (string.Compare(strA, rightName, StringComparison.Ordinal) == 0)
					{
						result = true;
						break;
					}
					intPtr2 = (IntPtr)((long)intPtr2 + (long)Marshal.SizeOf(typeof(NativeMethods.LSA_UNICODE_STRING)));
				}
			}
			finally
			{
				SafeNativeMethods.LsaFreeMemory(intPtr);
			}
			return result;
		}

		public override void CopyFromComponent(IComponent comp)
		{
		}

		private byte[] GetAccountSid(string accountName)
		{
			byte[] array = new byte[256];
			int[] array2 = new int[]
			{
				array.Length
			};
			char[] array3 = new char[1024];
			int[] domNameLen = new int[]
			{
				array3.Length
			};
			int[] sidNameUse = new int[1];
			if (accountName.Substring(0, 2) == ".\\")
			{
				StringBuilder stringBuilder = new StringBuilder(32);
				int num = 32;
				if (!NativeMethods.GetComputerName(stringBuilder, ref num))
				{
					throw new Win32Exception();
				}
				accountName = stringBuilder + accountName.Substring(1);
			}
			if (!NativeMethods.LookupAccountName(null, accountName, array, array2, array3, domNameLen, sidNameUse))
			{
				throw new Win32Exception();
			}
			byte[] array4 = new byte[array2[0]];
			Array.Copy(array, 0, array4, 0, array2[0]);
			return array4;
		}

		private void GetLoginInfo()
		{
			if (base.Context != null && !base.DesignMode)
			{
				if (this.haveLoginInfo)
				{
					return;
				}
				this.haveLoginInfo = true;
				if (this.serviceAccount == ServiceAccount.User)
				{
					if (base.Context.Parameters.ContainsKey("username"))
					{
						this.username = base.Context.Parameters["username"];
					}
					if (base.Context.Parameters.ContainsKey("password"))
					{
						this.password = base.Context.Parameters["password"];
					}
					if (this.username == null || this.username.Length == 0 || this.password == null)
					{
						if (!base.Context.Parameters.ContainsKey("unattended"))
						{
							using (ServiceInstallerDialog serviceInstallerDialog = new ServiceInstallerDialog())
							{
								if (this.username != null)
								{
									serviceInstallerDialog.Username = this.username;
								}
								serviceInstallerDialog.ShowDialog();
								switch (serviceInstallerDialog.Result)
								{
								case ServiceInstallerDialogResult.OK:
									this.username = serviceInstallerDialog.Username;
									this.password = serviceInstallerDialog.Password;
									return;
								case ServiceInstallerDialogResult.UseSystem:
									this.username = null;
									this.password = null;
									this.serviceAccount = ServiceAccount.LocalSystem;
									return;
								case ServiceInstallerDialogResult.Canceled:
									throw new InvalidOperationException(Res.GetString("UserCanceledInstall", new object[]
									{
										base.Context.Parameters["assemblypath"]
									}));
								default:
									return;
								}
							}
						}
						throw new InvalidOperationException(Res.GetString("UnattendedCannotPrompt", new object[]
						{
							base.Context.Parameters["assemblypath"]
						}));
					}
				}
			}
		}

		private static void GrantAccountRight(IntPtr policyHandle, byte[] accountSid, string rightName)
		{
			NativeMethods.LSA_UNICODE_STRING lSA_UNICODE_STRING = new NativeMethods.LSA_UNICODE_STRING();
			lSA_UNICODE_STRING.buffer = rightName;
			lSA_UNICODE_STRING.length = (short)(lSA_UNICODE_STRING.buffer.Length * 2);
			lSA_UNICODE_STRING.maximumLength = lSA_UNICODE_STRING.length;
			int num = NativeMethods.LsaAddAccountRights(policyHandle, accountSid, lSA_UNICODE_STRING, 1);
			if (num != 0)
			{
				throw new Win32Exception(SafeNativeMethods.LsaNtStatusToWinError(num));
			}
		}

		public override void Install(IDictionary stateSaver)
		{
			try
			{
				ServiceInstaller.CheckEnvironment();
				try
				{
					if (!this.haveLoginInfo)
					{
						try
						{
							this.GetLoginInfo();
						}
						catch
						{
							stateSaver["hadServiceLogonRight"] = true;
							throw;
						}
					}
				}
				finally
				{
					stateSaver["Account"] = this.Account;
					if (this.Account == ServiceAccount.User)
					{
						stateSaver["Username"] = this.Username;
					}
				}
				if (this.Account == ServiceAccount.User)
				{
					IntPtr intPtr = this.OpenSecurityPolicy();
					bool flag = true;
					try
					{
						byte[] accountSid = this.GetAccountSid(this.Username);
						flag = ServiceProcessInstaller.AccountHasRight(intPtr, accountSid, "SeServiceLogonRight");
						if (!flag)
						{
							ServiceProcessInstaller.GrantAccountRight(intPtr, accountSid, "SeServiceLogonRight");
						}
					}
					finally
					{
						stateSaver["hadServiceLogonRight"] = flag;
						SafeNativeMethods.LsaClose(intPtr);
					}
				}
			}
			finally
			{
				base.Install(stateSaver);
			}
		}

		private IntPtr OpenSecurityPolicy()
		{
			NativeMethods.LSA_OBJECT_ATTRIBUTES value = new NativeMethods.LSA_OBJECT_ATTRIBUTES();
			GCHandle gCHandle = GCHandle.Alloc(value, GCHandleType.Pinned);
			IntPtr result;
			try
			{
				IntPtr pointerObjectAttributes = gCHandle.AddrOfPinnedObject();
				IntPtr intPtr;
				int num = NativeMethods.LsaOpenPolicy(null, pointerObjectAttributes, 2064, out intPtr);
				if (num != 0)
				{
					throw new Win32Exception(SafeNativeMethods.LsaNtStatusToWinError(num));
				}
				result = intPtr;
			}
			finally
			{
				gCHandle.Free();
			}
			return result;
		}

		private static void RemoveAccountRight(IntPtr policyHandle, byte[] accountSid, string rightName)
		{
			NativeMethods.LSA_UNICODE_STRING lSA_UNICODE_STRING = new NativeMethods.LSA_UNICODE_STRING();
			lSA_UNICODE_STRING.buffer = rightName;
			lSA_UNICODE_STRING.length = (short)(lSA_UNICODE_STRING.buffer.Length * 2);
			lSA_UNICODE_STRING.maximumLength = lSA_UNICODE_STRING.length;
			int num = NativeMethods.LsaRemoveAccountRights(policyHandle, accountSid, false, lSA_UNICODE_STRING, 1);
			if (num != 0)
			{
				throw new Win32Exception(SafeNativeMethods.LsaNtStatusToWinError(num));
			}
		}

		public override void Rollback(IDictionary savedState)
		{
			try
			{
				if ((ServiceAccount)savedState["Account"] == ServiceAccount.User && !(bool)savedState["hadServiceLogonRight"])
				{
					string accountName = (string)savedState["Username"];
					IntPtr intPtr = this.OpenSecurityPolicy();
					try
					{
						byte[] accountSid = this.GetAccountSid(accountName);
						ServiceProcessInstaller.RemoveAccountRight(intPtr, accountSid, "SeServiceLogonRight");
					}
					finally
					{
						SafeNativeMethods.LsaClose(intPtr);
					}
				}
			}
			finally
			{
				base.Rollback(savedState);
			}
		}
	}
}
