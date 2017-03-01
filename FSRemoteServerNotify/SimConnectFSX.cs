using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace FSRemoteServerNotify
{

    public class SimConnectFSX : ISimConnect
    {
        SimConnect simconnect = null;
        private bool SimConnected;
        public const int WM_USER_SIMCONNECT = 0x0402;

        private IntPtr winHandle;
        private bool simExited = false;

        public event EventHandler Error;
        public event ConnectionEventHandler FSConnectionStatusChanged;
        public event SimConnectDataReceived OnSimConnectDataReceived;
 
        public SimConnectFSX(IntPtr handle)
        {
            winHandle = handle;
            SimConnected = false;
        }

        private void OnFSConnectionStatusChanged(bool connState)
        {
            if (FSConnectionStatusChanged != null)
            {
                FSConnectionStatusChanged(this, connState);
            }
        }

        private void OnDataReceived(DataReceivedEventArgs ar)
        {
            if (OnSimConnectDataReceived != null)
            {
                OnSimConnectDataReceived(this, ar);
            }
        }

        public void ConnectToSimConnect()
        {
            if(simconnect != null)
            {
                return;
            }

            try
            {
                simconnect = new SimConnect("FlySpadan Server", winHandle, WM_USER_SIMCONNECT, null, 0);
                initDataRequest();
            }
            catch (COMException ex)
            {
                // Couldn't connect to SimConnect
                OnError(simconnect, ex);
                return;
            }
            OnFSConnectionStatusChanged(true);
            SimConnected = true;
        }

        public void DisconnectSim()
        {
            if (simconnect != null)
            {
                OnFSConnectionStatusChanged(false);
                if (!simExited)
                {
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.SIM_START);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.SIM_STOP);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.FLIGHTPLAN_LOADED);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.FLIGHTPLAN_DIACTIVATED);
                }
                simconnect.Dispose();
                simconnect = null;
            }
            SimConnected = false;
        }

        /* ----------------------------------------------------------------------------------------------------------------------
        * ------------------------------------------- Error Handler method -----------------------------------------------------
        * ---------------------------------------------------------------------------------------------------------------------- */
        private void OnError(object sender, Exception ex)
        {
            EventHandler handler = Error;
            if (handler != null)
            {
                ErrorEventArgs e = new ErrorEventArgs(ex);
                handler(sender, e);
            }
        }

        public bool isConnected()
        {
            return (SimConnected);
        }

        public void GetFSMessages()
        {
            if (simconnect != null)
            {
                try
                {
                    simconnect.ReceiveMessage();
                }
                catch (COMException ex)
                {
                    // Exception means connection is lost, so the client side has to close
                    DisconnectSim();
                    OnError(this, ex);
                }

            }
        }

        private void initDataRequest()
        {
            try
            {
                simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simconnect_OnRecvOpen);
                simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);

                simconnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(simconnect_OnRecvError);

                //simconnect.AddToDataDefinition(DEFINITIONS.StructMapData, "title", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                //simconnect.AddToDataDefinition(DEFINITIONS.StructMapData, "Plane Altitude", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.StructMapData, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.StructMapData, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.StructMapData, "PLANE HEADING DEGREES TRUE", "Radians", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simconnect.RegisterDataDefineStruct<StructMapData>(DEFINITIONS.StructMapData);

                simconnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(simconnect_OnRecvSimobjectDataByType);
                simconnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(simconnect_OnRecvSimObjectData);
                simconnect.OnRecvSystemState += new SimConnect.RecvSystemStateEventHandler(simconnect_OnRecvSystemState);

                simconnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(simconnect_OnRecvEvent);
                simconnect.OnRecvEventFilename += new SimConnect.RecvEventFilenameEventHandler(simconnect_OnRecvEventFilename);

                simconnect.SubscribeToSystemEvent(EVENTS.SIM_START, "SimStart");
                simconnect.SubscribeToSystemEvent(EVENTS.SIM_STOP, "SimStop");
                simconnect.SubscribeToSystemEvent(EVENTS.FLIGHTPLAN_LOADED, "FlightPlanActivated");
                simconnect.SubscribeToSystemEvent(EVENTS.FLIGHTPLAN_DIACTIVATED, "FlightPlanDeactivated");

                simconnect.SetSystemEventState(EVENTS.SIM_START, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENTS.SIM_STOP, SIMCONNECT_STATE.ON);
                simconnect.SetSystemEventState(EVENTS.FLIGHTPLAN_LOADED, SIMCONNECT_STATE.OFF);
                simconnect.SetSystemEventState(EVENTS.FLIGHTPLAN_DIACTIVATED, SIMCONNECT_STATE.OFF);
            }
            catch (COMException ex)
            {
                OnError(this, ex);
            }
        }

        private void simconnect_OnRecvEventFilename(SimConnect sender, SIMCONNECT_RECV_EVENT_FILENAME data)
        {
            switch (data.uEventID)
            {
                case (uint)EVENTS.FLIGHTPLAN_LOADED:
                    FLIGHTPLAN_LINK fpl;
                    fpl.flightPlanFilename = data.szFileName;
                    OnDataReceived(new DataReceivedEventArgs(FSDataType.DT_FLIGHTPLAN, fpl));
                    break;

                default:
                    break;
            }
        }

        private void simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            FLIGHTSIM_STATUS_STRUCT fsStat;
            switch (data.uEventID)
            {
                case (uint)EVENTS.SIM_START:
                    fsStat.inFlight = true;
                    simconnect.SetSystemEventState(EVENTS.FLIGHTPLAN_LOADED, SIMCONNECT_STATE.ON);
                    OnDataReceived(new DataReceivedEventArgs(FSDataType.DT_FLIGHTSIM_STATUS, fsStat));
                    break;

                case (uint)EVENTS.SIM_STOP:
                    fsStat.inFlight = false;
                    OnDataReceived(new DataReceivedEventArgs(FSDataType.DT_FLIGHTSIM_STATUS, fsStat));
                    break;

                case (uint)EVENTS.FLIGHTPLAN_DIACTIVATED:
                    fsStat.inFlight = true;
                    OnDataReceived(new DataReceivedEventArgs(FSDataType.DT_RESTART, fsStat));
                    break;

                default:
                    break;
            }
        }

        private void simconnect_OnRecvSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE data)
        {
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_SYSTEMSTATE:
                    FLIGHTSIM_STATUS_STRUCT fsStat;
                    fsStat.inFlight = false;
                    if (data.dwInteger == 1)
                    {
                        fsStat.inFlight = true;
                        simconnect.SetSystemEventState(EVENTS.FLIGHTPLAN_LOADED, SIMCONNECT_STATE.ON);
                    }
                    OnDataReceived(new DataReceivedEventArgs(FSDataType.DT_FLIGHTSIM_STATUS, fsStat));
                    break;

                case DATA_REQUESTS.REQUEST_FLIGHTPLAN:
                    FLIGHTPLAN_LINK fpl;
                    fpl.flightPlanFilename = data.szString;
                    OnDataReceived(new DataReceivedEventArgs(FSDataType.DT_FLIGHTPLAN, fpl));
                    break;

                default:
                    break;
            }
        }

        private void simconnect_OnRecvSimObjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_MAPDATA:
                    StructMapData smd = (StructMapData)data.dwData[0];
                    MAP_DATA_STRUCT ds;
                    ds.latitude = smd.latitude;
                    ds.longitude = smd.longitude;
                    ds.heading = smd.heading;
                    DataReceivedEventArgs sArg = new DataReceivedEventArgs(FSDataType.DT_MAP, ds);
                    OnDataReceived(sArg);

                    break;

                default:
                    break;
            }
        }
        
        private void simconnect_OnRecvSimobjectDataByType(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_MAPDATA:
                    StructMapData smd = (StructMapData)data.dwData[0];
                    MAP_DATA_STRUCT ds;
                    ds.latitude = smd.latitude;
                    ds.longitude = smd.longitude;
                    ds.heading = smd.heading;
                    DataReceivedEventArgs sArg = new DataReceivedEventArgs(FSDataType.DT_MAP, ds);
                    OnDataReceived(sArg);
                    
                    break;

                default:
                    break;
            }
        }

        private void simconnect_OnRecvError(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            
        }

        private void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            simExited = true;
            OnFSConnectionStatusChanged(false);
        }

        private void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            OnFSConnectionStatusChanged(true);
        }

        public void SendRequest(FSDataType requestType)
        {
            if(simconnect == null)
            {
                return;
            }

            //simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_MAPDATA, DEFINITIONS.StructMapData, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

            switch (requestType)
            {
                case FSDataType.DT_MAP:
                    simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_MAPDATA, DEFINITIONS.StructMapData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                    break;

                case FSDataType.DT_FLIGHTSIM_STATUS:
                    simconnect.RequestSystemState(DATA_REQUESTS.REQUEST_SYSTEMSTATE, "Sim");
                    break;

                case FSDataType.DT_FLIGHTPLAN:
                    simconnect.RequestSystemState(DATA_REQUESTS.REQUEST_FLIGHTPLAN, "FlightPlan");
                    break;

                default:
                    break;
            }
        }

    }
}
