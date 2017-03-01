using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSRemoteServerNotify
{

    public delegate void SimConnectDataReceived(object sender, DataReceivedEventArgs arg);

    public enum DEFINITIONS
    {
        StructMapData,
    }

    public enum DATA_REQUESTS
    {
        REQUEST_MAPDATA,
        REQUEST_SYSTEMSTATE,
        REQUEST_FLIGHTPLAN,

    }

    public enum EVENTS
    {
        SIM_START,
        SIM_STOP,
        FLIGHTPLAN_LOADED,
        FLIGHTPLAN_DIACTIVATED,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StructMapData
    {
        public double latitude;
        public double longitude;
        public double heading;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct structPlaneInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public String title;

    }

    interface ISimConnect
    {
        void ConnectToSimConnect();
        void DisconnectSim();
        bool isConnected();
        void GetFSMessages();
        void SendRequest(FSDataType requestType);

        event EventHandler Error;
        event ConnectionEventHandler FSConnectionStatusChanged;
        event SimConnectDataReceived OnSimConnectDataReceived;
    }
}
