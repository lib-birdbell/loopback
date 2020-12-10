// ÆÄÀÏ FormGui.cs
using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Threading;	// Thread



namespace WindowsFormsApplication
{
    public partial class FormGui : Form
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName,string lpWindowName);

        [DllImport("user32.dll")]       
        static  extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		private List<CheckBox> lCheckBox = new List<CheckBox>();
		private List<TextBox> lTextBoxAddress = new List<TextBox>();
		private List<TextBox> lTextBoxSize = new List<TextBox>();
		private List<TextBox> lTextBoxFile = new List<TextBox>();
		private ISO7816 iso7816 = new ISO7816();
		private bool threadEnabled;
		private Int32 nCmdDelay;
		private delegate void buttonDelegate(int nTest1, int nTest2);
		Thread loopback;

        public FormGui()
        {
			// Hide console window
			IntPtr hWnd = FindWindow(null, Console.Title);
			if(hWnd != IntPtr.Zero){
				ShowWindow(hWnd, 0/*SW_HIDE*/);
			}

            InitializeComponent();
        }

		private void buttonConnect_Click(object sender, EventArgs e){
			string strComport;
			
			try{
				strComport = comportBox.SelectedItem.ToString();
			}catch(Exception es){
				MessageBox.Show("Com port name error!!\r\n" + es);
				return;
			}

			//MessageBox.Show("Com port : " + iso7816.GetComm() + " Baudrate : " + iso7816.GetBaudrate());

			if(iso7816.Open(comportBox.SelectedItem.ToString()) == false){
				MessageBox.Show("Com port : " + iso7816.GetComm() + " Open fail");
				MessageBox.Show("Error : " + iso7816.GetLastError());
				return;
			}
			
			this.buttonConnect.Enabled = false;
			this.buttonDisconnect.Enabled = true;
			this.buttonStart.Enabled = true;
			this.buttonStop.Enabled = false;
		}

		private void buttonDisconnect_Click(object sender, EventArgs e){
			iso7816.Close();
			
			this.buttonConnect.Enabled = true;
			this.buttonDisconnect.Enabled = false;
			this.buttonStart.Enabled = false;
			this.buttonStop.Enabled = false;
		}

		private void buttonStart_Click(object sender, EventArgs e){
			try{
				nCmdDelay = Int32.Parse(textBoxCmdDelay.Text);
			}catch(ArgumentNullException ex){
				MessageBox.Show("error!!\r\n" + ex);
				return;
			}catch(ArgumentException ex){
				MessageBox.Show("error!!\r\n" + ex);
				return;
			}catch(FormatException ex){
				MessageBox.Show("error!!\r\n" + ex);
				return;
			}catch(OverflowException ex){
				MessageBox.Show("error!!\r\n" + ex);
				return;
			}

			this.buttonConnect.Enabled = false;
			this.buttonDisconnect.Enabled = false;
			this.buttonStart.Enabled = false;
			this.buttonStop.Enabled = true;

			// Start thread
			threadEnabled = true;
			loopback = new Thread(new ThreadStart(Run));
			loopback.Start();
		}

		private void buttonStop_Click(object sender, EventArgs e){
			this.buttonConnect.Enabled = false;
			this.buttonDisconnect.Enabled = true;
			this.buttonStart.Enabled = true;
			this.buttonStop.Enabled = false;

			threadEnabled = false;
			//loopback.Interrupt();
		}

		private void buttonClear_Click(object sender, EventArgs e){
			textBoxLog.Text = "";
		}
		private void setButtonReadyToStart(int nTest1, int nTest2){
			this.buttonConnect.Enabled = false;
			this.buttonDisconnect.Enabled = true;
			this.buttonStart.Enabled = true;
			this.buttonStop.Enabled = false;
		}

		/*
		 * Remarks	: Run loopback thread	======================================================================
		 */
		private void Run(){
			bool iccPresent, iccActive;
			iccPresent = false;
			iccActive = false;

		//try{
			while(threadEnabled){
				RS232Data recvData = new RS232Data();
				byte[] tempByte;
				byte[] atrByte;

				// 1> GetSlotStatus ==========
				try{
					recvData = iso7816.GetSlotStatus();
				}catch(Exception e){
					MessageBox.Show("error!!\r\n" + e);
				}
				if(recvData.nStatus < 0){
					textBoxLog.Text += ">GetSlotStatus error!!" + recvData.nStatus + "\r\n";
					//break;
				}else{
					textBoxLog.Text += ">GetSlotStatus\r\n";
					tempByte = new byte[recvData.nLength];
					Array.Copy(recvData.data, tempByte, recvData.nLength);
					textBoxLog.Text += BitConverter.ToString(tempByte);
					textBoxLog.Text += "\r\n";
					// ICC error
					if(tempByte[8] != 0x00){
						textBoxLog.Text += ISO7816.GetErrorStr(tempByte[8]);
						break;
					}
					// Status issues
					if(Convert.ToBoolean(tempByte[7] & 0xC0)){
						textBoxLog.Text += ISO7816.GetStatusStr(tempByte[7]);
						break;
					}
					// No ICC is present
					if((byte)(tempByte[7] & 0x02) == 0x02){
						textBoxLog.Text += ISO7816.GetStatusStr(tempByte[7]);
						break;
					}
					iccPresent = true;
				}

				Thread.Sleep(nCmdDelay);

				// 2> APDU
				// An ICC is persent and inactive (not activated or shut down by hardware error)
				if(iccPresent && (iccActive == false)){
					// 2-A> Power on ==========
					try{
						recvData = iso7816.PowerOn();
					}catch(Exception e){
						MessageBox.Show("error!!\r\n" + e);
					}
					if(recvData.nStatus < 0){
						textBoxLog.Text += ">PowerOn error!!" + recvData.nStatus + "\r\n";
						//break;
					}else{
						textBoxLog.Text += ">PowerOn\r\n";
						tempByte = new byte[recvData.nLength];
						Array.Copy(recvData.data, tempByte, recvData.nLength);
						textBoxLog.Text += BitConverter.ToString(tempByte);
						textBoxLog.Text += "\r\n";
						// ICC error
						if(tempByte[8] != 0x00){
							textBoxLog.Text += ISO7816.GetErrorStr(tempByte[8]);
							break;
						}
						// Status issues
						if(Convert.ToBoolean(tempByte[7] & 0xC0)){
							textBoxLog.Text += ISO7816.GetStatusStr(tempByte[7]);
							break;
						}
						// No ICC is present or An ICC is persent and inactive (not activated or shut down by hardware error)
						if((byte)(tempByte[7] & 0x03) != 0x00){
							textBoxLog.Text += ISO7816.GetStatusStr(tempByte[7]);
							break;
						}
						iccActive = true;

						Int32 length;
						length = ISO7816.GetDataLength(tempByte);
						if(length > 0){
							atrByte = new byte[length];
							Array.Copy(recvData.data, 10, atrByte, 0, length);
							textBoxLog.Text += "ATR ==>";
							textBoxLog.Text += BitConverter.ToString(atrByte);
							textBoxLog.Text += "\r\n";

							// Check TS T0
							if(atrByte[0] == 0x3B){			// Direct Convention
								if((byte)(atrByte[1] & 0xF0) == 0xE0){
									textBoxLog.Text += "Protocol : T1\r\n";
								}else{
									textBoxLog.Text += "Protocol : T0\r\n";
								}
							}else if(atrByte[0] == 0x3F){	// Inverse Convention
								if((byte)(atrByte[1] & 0xF0) == 0xF0){
									textBoxLog.Text += "Protocol : T1\r\n";
								}else{
									textBoxLog.Text += "Protocol : T0\r\n";
								}
							}
						}
					}
				}else if(iccPresent){
					//
				}

				Thread.Sleep(nCmdDelay);

				// 3> APDU
				if(iccPresent){
					try{
						byte[] xfrBlock = new byte[] {0x00, 0xA4, 0x04, 0x00, 0x0E, 0x31, 0x50, 0x41, 0x59, 0x2E, 0x53, 0x59, 0x53, 0x2E, 0x44, 0x44, 0x46, 0x30, 0x31, 0x00};
						recvData = iso7816.XfrBlock(xfrBlock, 20);
						textBoxLog.Text += "C_APDU ==>";
						textBoxLog.Text += BitConverter.ToString(xfrBlock);
						textBoxLog.Text += "\r\n";
					}catch(Exception e){
						MessageBox.Show("error!!\r\n" + e);
					}
					if(recvData.nStatus < 0){
						textBoxLog.Text += ">APDU error!!" + recvData.nStatus + "\r\n";
						break;
					}else{
						textBoxLog.Text += ">APDU\r\n";
						tempByte = new byte[recvData.nLength];
						Array.Copy(recvData.data, tempByte, recvData.nLength);
						textBoxLog.Text += BitConverter.ToString(tempByte);
						textBoxLog.Text += "\r\n";

						// Get R_APDU
						Int32 length;
						length = ISO7816.GetDataLength(tempByte);
						if(length > 0){
							atrByte = new byte[length];
							Array.Copy(recvData.data, 10, atrByte, 0, length);
							textBoxLog.Text += "R_APDU <==";
							textBoxLog.Text += BitConverter.ToString(atrByte);
							textBoxLog.Text += "\r\n";
						}
					}
				}

				// Last> PowerOff
				if(iccPresent && iccActive){
					// Last> Power off ==========
					try{
						recvData = iso7816.PowerOff();
					}catch(Exception e){
						MessageBox.Show("error!!\r\n" + e);
					}
					if(recvData.nStatus < 0){
						textBoxLog.Text += ">PowerOff error!!" + recvData.nStatus + "\r\n";
						break;
					}else{
						textBoxLog.Text += ">PowerOff\r\n";
						tempByte = new byte[recvData.nLength];
						Array.Copy(recvData.data, tempByte, recvData.nLength);
						textBoxLog.Text += BitConverter.ToString(tempByte);
						textBoxLog.Text += "\r\n";
						// ICC error
						if(tempByte[8] != 0x00){
							textBoxLog.Text += ISO7816.GetErrorStr(tempByte[8]);
							break;
						}
						// Status issues
						if(Convert.ToBoolean(tempByte[7] & 0xC0)){
							textBoxLog.Text += ISO7816.GetStatusStr(tempByte[7]);
							break;
						}
						// No ICC is present
						if((byte)(tempByte[7] & 0x03) == 0x02){
							textBoxLog.Text += ISO7816.GetStatusStr(tempByte[7]);
							break;
						}
						// An ICC is persent and inactive (not activated or shut down by hardware error)
						if((byte)(tempByte[7] & 0x03) == 0x01){
							iccActive = false;
							iccPresent = true;
						}else{
							iccActive = true;
							iccPresent = true;
						}
					}
					textBoxLog.Text += "\r\n";
				}

				Thread.Sleep(nCmdDelay);
			}// End of while
		//}catch(ThreadInterruptedException e){
		//}finally{

			this.Invoke(new buttonDelegate(setButtonReadyToStart), new object[] {0, 0});
		//}// End of try
		
		}
		/* End of Run()	======================================================================*/
    }
}



// File FormGui.Designer.cs
namespace WindowsFormsApplication
{
    partial class FormGui
    {
        private System.ComponentModel.IContainer components = null;
		private Button buttonConnect;
		private Button buttonDisconnect;
		private Button buttonStart;
		private Button buttonStop;
		private Button buttonClear;
		private TextBox textBoxCmdDelay;
		private TextBox textBoxLog;
		private ComboBox comportBox;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
			//System.Windows.Forms.Button buttonStart;
			this.buttonStart = new System.Windows.Forms.Button();
			this.buttonConnect = new Button();
			this.buttonDisconnect = new Button();
			this.buttonStop = new Button();
			this.buttonClear = new Button();
			Label labelComm = new Label();
			this.textBoxCmdDelay = new TextBox();
			this.textBoxLog = new TextBox();
			this.comportBox = new ComboBox();
			
			Label labelAddress = new Label();
			Label labelSize = new Label();
			Label labelFile = new Label();

			this.SuspendLayout();

			// FormGui
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Text = "AK9564";
			this.ClientSize = new System.Drawing.Size(640, 320);
			this.Controls.Add(buttonConnect);
			this.Controls.Add(buttonDisconnect);
			this.Controls.Add(this.buttonStart);
			this.Controls.Add(buttonStop);
			this.Controls.Add(buttonClear);
			this.Controls.Add(labelComm);
			//this.Controls.Add(labelAddress);
			this.Controls.Add(textBoxCmdDelay);
			this.Controls.Add(textBoxLog);
			this.Controls.Add(comportBox);

			// Button [CONNECT]
			this.buttonConnect.Location = new System.Drawing.Point(530, 10);
			//this.button.Location = new Point(367, 174);
			this.buttonConnect.Name = "buttonConnect";
			this.buttonConnect.Size = new System.Drawing.Size(100, 23);
			this.buttonConnect.TabIndex = 0;
			this.buttonConnect.Text = "CONNECT";
			this.buttonConnect.UseVisualStyleBackColor = true;
			this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);

			// Button [DISCONNECT]
			this.buttonDisconnect.Location = new System.Drawing.Point(530, 40);
			this.buttonDisconnect.Name = "buttonDisconnect";
			this.buttonDisconnect.Size = new System.Drawing.Size(100, 23);
			this.buttonDisconnect.TabIndex = 1;
			this.buttonDisconnect.Text = "DISCONNECT";
			this.buttonDisconnect.UseVisualStyleBackColor = true;
			this.buttonDisconnect.Enabled = false;
			this.buttonDisconnect.Click += new System.EventHandler(this.buttonDisconnect_Click);

			// Button [START]
			this.buttonStart.Location = new System.Drawing.Point(530, 70);
			//this.buttonStart.Location = new Point(367, 174);
			this.buttonStart.Name = "buttonStart";
			this.buttonStart.Size = new System.Drawing.Size(100, 23);
			this.buttonStart.TabIndex = 2;
			this.buttonStart.Text = "START";
			this.buttonStart.UseVisualStyleBackColor = true;
			this.buttonStart.Enabled = false;
			this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);

			// Button [STOP]
			this.buttonStop.Location = new System.Drawing.Point(530, 100);
			this.buttonStop.Name = "buttonStop";
			this.buttonStop.Size = new System.Drawing.Size(100, 23);
			this.buttonStop.TabIndex = 3;
			this.buttonStop.Text = "STOP";
			this.buttonStop.UseVisualStyleBackColor = true;
			this.buttonStop.Enabled = false;
			this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);

			// Button [CLEAR]
			this.buttonClear.Location = new System.Drawing.Point(530, 200);
			this.buttonClear.Name = "buttonClear";
			this.buttonClear.Size = new System.Drawing.Size(100, 23);
			//this.buttonClear.TabIndex = 3;
			this.buttonClear.Text = "CLEAR";
			this.buttonClear.UseVisualStyleBackColor = true;
			this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);

			// Label [COM]
			labelComm.Location = new System.Drawing.Point(380, 75);
			labelComm.Name = "labelComm";
			labelComm.Size = new System.Drawing.Size(45, 20);
			//labelComm.TabIndex = 5;
			labelComm.Text = "COM :";

/*
			// Label
			labelName.Location = new System.Drawing.Point(30, 40);
			labelName.Name = "labelName";
			labelName.Size = new System.Drawing.Size(55, 20);
			labelName.Text = "NAME";
*/
			// TextBox [CmdDelay]
			this.textBoxCmdDelay.Location = new System.Drawing.Point(430, 40);
			this.textBoxCmdDelay.Size = new System.Drawing.Size(60, 20);
			//this.textBoxCmdDelay.TabIndex = 4;
			this.textBoxCmdDelay.TextAlign = HorizontalAlignment.Center;
			this.textBoxCmdDelay.Text = "80";

			// TextBox [Log]
			this.textBoxLog.Location = new System.Drawing.Point(10, 70);
			this.textBoxLog.Size = new System.Drawing.Size(300, 200);
			this.textBoxLog.AcceptsReturn = true;
			this.textBoxLog.AcceptsTab = true;
			this.textBoxLog.Multiline = true;
			this.textBoxLog.ReadOnly = true;
			this.textBoxLog.MaxLength = 10;
			this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			//this.textBoxLog.Text = "test\r\nABCD\r\n";	// Test text

			// ComboBox [COM]
			comportBox.Location = new System.Drawing.Point(430, 70);
			comportBox.Size = new System.Drawing.Size(80, 20);
			comportBox.Name = "comportComboBox";
			// Get communication port and put list
			string[] PortNames = ISO7816.GetPorts();

			foreach(string portnumber in PortNames){
				comportBox.Items.Add(portnumber);
			}

			this.ResumeLayout(false);
        }

        #endregion
    }
}

