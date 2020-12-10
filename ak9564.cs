using System;
using System.Windows.Forms;

namespace WindowsFormsApplication
{
	static class Program
	{
		[STAThread]
		static void Main () {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FormGui());
		}
	}
}
