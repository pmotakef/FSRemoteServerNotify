using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Net.Sockets;


namespace FSRemoteServerNotify
{
    public enum FlightSimType
    {
        FST_P3D = 1,
        FST_FSX = 2,
    }

public partial class FSSetupPage : Form
    {

        private TCPServer FSRemoteServer = null;
        private ISimConnect simConnectApp = null;
        private bool bInFlight;
        private Timer connectionTimer = null;
        private int hostPortNum;
        private FlightSimType fsType;
        private const int WM_USER_SIMCONNECT = 0x0402;

        public FSSetupPage()
        {
            InitializeComponent();
            this.Hide();
            niServer.Visible = true;
            WindowState = FormWindowState.Minimized;
            tbIPAddress.Text = TCPFunctions.get_IP_address();
            bInFlight = false;

            connectionTimer = new Timer();
            connectionTimer.Interval = 5000;
            connectionTimer.Tick += new EventHandler(OnConnectionTimerElapsed);
            connectionTimer.Start();

            hostPortNum = (int)Properties.Settings.Default["hostPortNumber"];
            fsType = (FlightSimType)((int)Properties.Settings.Default["FSTypeDefault"]);
            if (fsType == FlightSimType.FST_FSX)
            {
                mnuFSX.Checked = true;
                mnuP3D.Checked = false;
            }
            else if (fsType == FlightSimType.FST_P3D)
            {
                mnuFSX.Checked = false;
                mnuP3D.Checked = true;
            }
            tbPortNum.Text = hostPortNum.ToString();
            StartListener();
        }

        private void OnConnectionTimerElapsed(object sender, EventArgs e)
        {
            if (!FSRemoteServer.IsConnected())
            {
                return;
            }


            if (simConnectApp != null)
            {
                if (simConnectApp.isConnected())
                {
                    if (!bInFlight)
                    {
                        simConnectApp.SendRequest(FSDataType.DT_FLIGHTSIM_STATUS);
                        return;
                    }
                    else
                    {
                        simConnectApp.SendRequest(FSDataType.DT_FLIGHTPLAN);
                        simConnectApp.SendRequest(FSDataType.DT_MAP);
                        connectionTimer.Stop();
                        return;
                    }
                }
                else
                {
                    ConnectToSim();
                }
            }
            else
            {
                ConnectToSim();
            } 
        }

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT && simConnectApp != null)
            {
                if (simConnectApp.isConnected())
                {
                    simConnectApp.GetFSMessages();
                }
            }
            else
            {
                base.DefWndProc(ref m);
            }
        }

        private void FSSetupPage_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                niServer.Visible = true;
                //niServer.ShowBalloonTip(500);
                this.Hide();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExitServer();
        }

        private void smiSetup_Click(object sender, EventArgs e)
        {
            this.Show();
            WindowState = FormWindowState.Normal;
            this.Focus();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Hide();
            WindowState = FormWindowState.Minimized;
        }

        private void ExitServer()
        {
            Properties.Settings.Default["hostPortNumber"] = hostPortNum;
            Properties.Settings.Default["FSTypeDefault"] = (int)fsType;
            Properties.Settings.Default.Save();
            if (simConnectApp != null)
            {
                DisconnectFromFS();
            }
            stopListener();
            if(connectionTimer.Enabled)
            {
                connectionTimer.Stop();
                connectionTimer.Dispose();
            }
            this.Close();
        }

        private void smiAbout_Click(object sender, EventArgs e)
        {
            AboutWindow abtWin = new AboutWindow();
            abtWin.Show();
        }


        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // -----------------------------------------------------------     Client Part    --------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------

        private void StartListener()
        {
            FSRemoteServer = new TCPServer(IPAddress.Parse(TCPFunctions.get_IP_address()), hostPortNum, 0);
            FSRemoteServer.ClientConnectionEstablished += new ConnectionEventHandler(updateClientConnectionState);
            FSRemoteServer.DataTransmissionRecieved += new DataRecieved(handleClientDataRecieved);
            FSRemoteServer.OnError += new ErrorHandler(OnServerConnectionError);
            try
            {
                FSRemoteServer.asyncInit();
            }
            catch
            {
                System.Windows.MessageBox.Show("Couldn't find network.");
                return;
            }
        }

        private void OnServerConnectionError(object sender, ErrorEventArgs e)
        {
            if (e.GetException() is SocketException)
            {
                MessageBox.Show("Couldn't find network!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void handleClientDataRecieved(object sender, byte[] buffer)
        {
            switch (buffer[0])
            {
                case (byte)FSDataType.DT_CLIENTTYPE:
                    string myStr = DataManager.GetClientTypeString((ClientCode)buffer[1]) + " " + buffer[2] + "." + buffer[3] + "." + buffer[4];
                    smiClientConnect.Text = myStr + ": Connected";
                    break;

                default:
                    break;
            }
        }

        private void updateClientConnectionState(object sender, bool state)
        {
            if (state)
            {
                smiClientConnect.Text = "Client: Connected";
                smiClientConnect.Image = new Bitmap(Properties.Resources.connected);

                byte[] sData = new byte[2];
                sData[0] = 1;
                sData[1] = (byte)FSDataType.DT_RESTART;
                FSRemoteServer.sendData(sData);
                mnuP3D.Enabled = false;
                mnuFSX.Enabled = false;
            }
            else
            {
                smiClientConnect.Text = "Client: Not Connected";
                stopListener();
                if(simConnectApp != null)
                {
                    DisconnectFromFS();
                }
                StartListener();
                smiClientConnect.Image = new Bitmap(Properties.Resources.disconnected);
                connectionTimer.Start();
                mnuP3D.Enabled = true;
                mnuFSX.Enabled = true;
            }
        }

        private void stopListener()
        {
            if (FSRemoteServer == null)
            {
                return;
            }
            if (FSRemoteServer.IsAlive())
            {
                if (FSRemoteServer.IsConnected())
                {
                    FSRemoteServer.stopRecieving();
                }
                FSRemoteServer.Stop();
            }
        }


        private void btnSetPort_Click(object sender, EventArgs e)
        {
            if (FSRemoteServer != null)
            {
                if (FSRemoteServer.IsConnected())
                {
                    DialogResult res = MessageBox.Show("A Client is already connected. Changing the Port number will disconnect the client. Do you want to continue with port number change?", "Warning! Client is about to be disconnected.", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
                    if (res == DialogResult.No)
                    {
                        return;
                    }
                }
            }
            if(simConnectApp != null)
            {
                DisconnectFromFS();
            }
            if(FSRemoteServer != null)
            {
                stopListener();
                FSRemoteServer = null;
            }
            hostPortNum = Convert.ToInt32(tbPortNum.Text);
            StartListener();
            connectionTimer.Start();
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // -------------------------------------------------------     Flight Sim Part    --------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------------------------------------------------------


        private void SimConnectApp_FSConnectionStatusChanged(object sender, bool state)
        {
            if (state)
            {
                smiFSConnect.Text = "SimConnect: Connected";
                smiFSConnect.Image = new Bitmap(Properties.Resources.connected);
            }
            else
            {
                smiFSConnect.Text = "SimConnect: Not Connected";
                smiFSConnect.Image = new Bitmap(Properties.Resources.disconnected);
                if (simConnectApp != null)
                {
                    simConnectApp = null;
                }
                connectionTimer.Start();
            }
        }

        private void ConnectToSim()
        {
            if(simConnectApp != null)
            {
                DisconnectFromFS();
            }
            
            if (fsType == FlightSimType.FST_P3D)
            {
                simConnectApp = new SimConnectP3D(this.Handle);
            }
            else if (fsType == FlightSimType.FST_FSX)
            {
                simConnectApp = new SimConnectFSX(this.Handle);
            }
            else
            {
                return;
            }
            simConnectApp.FSConnectionStatusChanged += new ConnectionEventHandler(SimConnectApp_FSConnectionStatusChanged);
            simConnectApp.OnSimConnectDataReceived += new SimConnectDataReceived(SimConnectApp_SimDataReceived);
            simConnectApp.ConnectToSimConnect();
        }

        private void SimConnectApp_SimDataReceived(object sender, DataReceivedEventArgs arg)
        {
            if (!FSRemoteServer.IsConnected())
            {
                return;
            }

            if (arg.DataType == FSDataType.DT_FLIGHTSIM_STATUS)
            {
                FLIGHTSIM_STATUS_STRUCT tp = (FLIGHTSIM_STATUS_STRUCT)arg.eventData;
              /*  if(bInFlight && !tp.inFlight)
                {
                    byte[] sData = new byte[2];
                    sData[0] = 1;
                    sData[1] = (byte)FSDataType.DT_RESTART;
                    FSRemoteServer.sendData(sData);
                } */
                bInFlight = tp.inFlight;
            }
            else
            {
                List<byte[]> sendData = DataManager.convertDataStructToByte(arg.DataType, arg.eventData).ToList();
                if (sendData.Count < 1)
                {
                    return;
                }
                for (int i = 0; i < sendData.Count; i++)
                {
                    FSRemoteServer.sendData(sendData[i]);
                }
            }

        }

        private void DisconnectFromFS()
        {
            simConnectApp.DisconnectSim();
            simConnectApp = null;
        }

        private void mnuP3D_Click(object sender, EventArgs e)
        {
            mnuP3D.Checked = true;
            mnuFSX.Checked = false;
            fsType = FlightSimType.FST_P3D;
        }

        private void mnuFSX_Click(object sender, EventArgs e)
        {
            mnuFSX.Checked = true;
            mnuP3D.Checked = false;
            fsType = FlightSimType.FST_FSX;
        }
    }
}
