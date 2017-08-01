using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace System.ServiceProcess.Design
{
	public class ServiceInstallerDialog : Form
	{
		private Button okButton;

		private TextBox passwordEdit;

		private Button cancelButton;

		private TextBox confirmPassword;

		private TextBox usernameEdit;

		private Label label1;

		private Label label2;

		private Label label3;

		private TableLayoutPanel okCancelTableLayoutPanel;

		private TableLayoutPanel overarchingTableLayoutPanel;

		private ServiceInstallerDialogResult result;

		public string Password
		{
			get
			{
				return this.passwordEdit.Text;
			}
			set
			{
				this.passwordEdit.Text = value;
			}
		}

		public ServiceInstallerDialogResult Result
		{
			get
			{
				return this.result;
			}
		}

		public string Username
		{
			get
			{
				return this.usernameEdit.Text;
			}
			set
			{
				this.usernameEdit.Text = value;
			}
		}

		public ServiceInstallerDialog()
		{
			this.InitializeComponent();
		}

		[STAThread]
		public static void Main()
		{
			Application.Run(new ServiceInstallerDialog());
		}

		private void InitializeComponent()
		{
			ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(ServiceInstallerDialog));
			this.okButton = new Button();
			this.passwordEdit = new TextBox();
			this.cancelButton = new Button();
			this.confirmPassword = new TextBox();
			this.usernameEdit = new TextBox();
			this.label1 = new Label();
			this.label2 = new Label();
			this.label3 = new Label();
			this.okCancelTableLayoutPanel = new TableLayoutPanel();
			this.overarchingTableLayoutPanel = new TableLayoutPanel();
			this.okCancelTableLayoutPanel.SuspendLayout();
			this.overarchingTableLayoutPanel.SuspendLayout();
			base.SuspendLayout();
			componentResourceManager.ApplyResources(this.okButton, "okButton");
			this.okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.okButton.DialogResult = DialogResult.OK;
			this.okButton.Margin = new Padding(0, 0, 3, 0);
			this.okButton.MinimumSize = new Size(75, 23);
			this.okButton.Name = "okButton";
			this.okButton.Padding = new Padding(10, 0, 10, 0);
			this.okButton.Click += new EventHandler(this.okButton_Click);
			componentResourceManager.ApplyResources(this.passwordEdit, "passwordEdit");
			this.passwordEdit.Margin = new Padding(3, 3, 0, 3);
			this.passwordEdit.Name = "passwordEdit";
			componentResourceManager.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.cancelButton.DialogResult = DialogResult.Cancel;
			this.cancelButton.Margin = new Padding(3, 0, 0, 0);
			this.cancelButton.MinimumSize = new Size(75, 23);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Padding = new Padding(10, 0, 10, 0);
			this.cancelButton.Click += new EventHandler(this.cancelButton_Click);
			componentResourceManager.ApplyResources(this.confirmPassword, "confirmPassword");
			this.confirmPassword.Margin = new Padding(3, 3, 0, 3);
			this.confirmPassword.Name = "confirmPassword";
			componentResourceManager.ApplyResources(this.usernameEdit, "usernameEdit");
			this.usernameEdit.Margin = new Padding(3, 0, 0, 3);
			this.usernameEdit.Name = "usernameEdit";
			componentResourceManager.ApplyResources(this.label1, "label1");
			this.label1.Margin = new Padding(0, 0, 3, 3);
			this.label1.Name = "label1";
			componentResourceManager.ApplyResources(this.label2, "label2");
			this.label2.Margin = new Padding(0, 3, 3, 3);
			this.label2.Name = "label2";
			componentResourceManager.ApplyResources(this.label3, "label3");
			this.label3.Margin = new Padding(0, 3, 3, 3);
			this.label3.Name = "label3";
			componentResourceManager.ApplyResources(this.okCancelTableLayoutPanel, "okCancelTableLayoutPanel");
			this.okCancelTableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.overarchingTableLayoutPanel.SetColumnSpan(this.okCancelTableLayoutPanel, 2);
			this.okCancelTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			this.okCancelTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			this.okCancelTableLayoutPanel.Controls.Add(this.okButton, 0, 0);
			this.okCancelTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
			this.okCancelTableLayoutPanel.Margin = new Padding(0, 6, 0, 0);
			this.okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel";
			this.okCancelTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
			componentResourceManager.ApplyResources(this.overarchingTableLayoutPanel, "overarchingTableLayoutPanel");
			this.overarchingTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			this.overarchingTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			this.overarchingTableLayoutPanel.Controls.Add(this.label1, 0, 0);
			this.overarchingTableLayoutPanel.Controls.Add(this.okCancelTableLayoutPanel, 0, 3);
			this.overarchingTableLayoutPanel.Controls.Add(this.label2, 0, 1);
			this.overarchingTableLayoutPanel.Controls.Add(this.confirmPassword, 1, 2);
			this.overarchingTableLayoutPanel.Controls.Add(this.label3, 0, 2);
			this.overarchingTableLayoutPanel.Controls.Add(this.passwordEdit, 1, 1);
			this.overarchingTableLayoutPanel.Controls.Add(this.usernameEdit, 1, 0);
			this.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel";
			this.overarchingTableLayoutPanel.RowStyles.Add(new RowStyle());
			this.overarchingTableLayoutPanel.RowStyles.Add(new RowStyle());
			this.overarchingTableLayoutPanel.RowStyles.Add(new RowStyle());
			this.overarchingTableLayoutPanel.RowStyles.Add(new RowStyle());
			base.AcceptButton = this.okButton;
			componentResourceManager.ApplyResources(this, "$this");
			base.AutoScaleMode = AutoScaleMode.Font;
			base.AutoScaleDimensions = new SizeF(6f, 13f);
			base.CancelButton = this.cancelButton;
			base.Controls.Add(this.overarchingTableLayoutPanel);
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.HelpButton = true;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "ServiceInstallerDialog";
			base.ShowIcon = false;
			base.ShowInTaskbar = false;
			base.HelpButtonClicked += new CancelEventHandler(this.ServiceInstallerDialog_HelpButtonClicked);
			this.okCancelTableLayoutPanel.ResumeLayout(false);
			this.okCancelTableLayoutPanel.PerformLayout();
			this.overarchingTableLayoutPanel.ResumeLayout(false);
			this.overarchingTableLayoutPanel.PerformLayout();
			base.ResumeLayout(false);
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			this.result = ServiceInstallerDialogResult.Canceled;
			base.DialogResult = DialogResult.Cancel;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			this.result = ServiceInstallerDialogResult.OK;
			if (this.passwordEdit.Text == this.confirmPassword.Text)
			{
				base.DialogResult = DialogResult.OK;
				return;
			}
			MessageBoxOptions options = (MessageBoxOptions)0;
			Control control = this;
			while (control.RightToLeft == RightToLeft.Inherit)
			{
				control = control.Parent;
			}
			if (control.RightToLeft == RightToLeft.Yes)
			{
				options = (MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
			}
			base.DialogResult = DialogResult.None;
			MessageBox.Show(Res.GetString("Label_MissmatchedPasswords"), Res.GetString("Label_SetServiceLogin"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, options);
			this.passwordEdit.Text = string.Empty;
			this.confirmPassword.Text = string.Empty;
			this.passwordEdit.Focus();
		}

		private void ServiceInstallerDialog_HelpButtonClicked(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
		}
	}
}
