using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct ButtonStruct
{
    public bool state;
    public int code;
}

namespace FSMapClient

{

    public enum FSDataType
    {
        DT_INT = 0,
        DT_FLOAT = 1,
        DT_CLIENTTYPE = 2,
        DT_MAP = 3,
        DT_FLIGHTSIM_STATUS = 4,
        DT_FLIGHTPLAN = 5,
        DT_RESTART = 6,
    }

    public enum ClientCode
    {
        CC_MAP_INFO = 1,
    }

    public struct CLIENT_TYPE
    {
        public ClientCode ccode;
        public byte ReleaseVersion;
        public byte MajorVersion;
        public byte MinorVersion;
    }

    public struct MAP_DATA_STRUCT
    {
        public double latitude;
        public double longitude;
        public double heading;
    }

    public struct FLIGHTSIM_STATUS_STRUCT
    {
        public bool inFlight;
    }

    public struct FLIGHTPLAN_LINK
    {
        public string flightPlanFilename;
    }

    public static class DataManager
    {

        public static IEnumerable<byte[]> convertDataStructToByte(FSDataType dType, object data)
        {
            List<byte[]> retData = new List<byte[]>();
            // byte[0] is the packet size to be sent not including the packet size.
            byte[] sData = null;

            switch (dType)
            {
                case FSDataType.DT_MAP:
                    byte[] lat = BitConverter.GetBytes(((MAP_DATA_STRUCT)data).latitude);
                    byte[] lon = BitConverter.GetBytes(((MAP_DATA_STRUCT)data).longitude);
                    byte[] hed = BitConverter.GetBytes(((MAP_DATA_STRUCT)data).heading);
                    sData = new byte[(sizeof(double) * 3) + 2];
                    sData[0] = (byte)(sData.Length - 1);
                    sData[1] = (byte)FSDataType.DT_MAP;
                    for (int i = 0; i < (sizeof(double)); i++)
                    {
                        sData[i + 2] = lat[i];
                        sData[i + (sizeof(double)) + 2] = lon[i];
                        sData[i + (sizeof(double) * 2) + 2] = hed[i];
                    }
                    retData.Add(sData);
                    break;

                case FSDataType.DT_FLIGHTPLAN:
                    FlightPlanParser prs = new FlightPlanParser(((FLIGHTPLAN_LINK)data).flightPlanFilename);
                    if (prs.waypoints == null)
                    {
                        break;
                    }
                    for (int i = 0; i < prs.waypoints.Count; i++)
                    {
                        byte[] pData = null;
                        pData = new byte[2 * sizeof(double) + 3];
                        pData[0] = (byte)(pData.Length - 1);
                        pData[1] = (byte)FSDataType.DT_FLIGHTPLAN;
                        pData[2] = (byte)(prs.waypoints.Count - (i + 1));
                        byte[] lt = BitConverter.GetBytes(prs.waypoints[i].latitude);
                        byte[] ln = BitConverter.GetBytes(prs.waypoints[i].longitude);
                        for (int j = 0; j < sizeof(double); j++)
                        {
                            pData[3 + j] = lt[j];
                            pData[(sizeof(double)) + 3 + j] = ln[j];
                        }
                        retData.Add(pData);
                    }
                    break;

                case FSDataType.DT_RESTART:
                    sData = new byte[2];
                    sData[0] = 1;
                    sData[1] = (byte)FSDataType.DT_RESTART;
                    retData.Add(sData);
                    break;

                case FSDataType.DT_CLIENTTYPE:
                    CLIENT_TYPE ct = (CLIENT_TYPE)data;
                    sData = new byte[6];
                    sData[0] = (byte)(sData.Length - 1);
                    sData[1] = (byte)FSDataType.DT_CLIENTTYPE;
                    sData[2] = (byte)ct.ccode;
                    sData[3] = ct.ReleaseVersion;
                    sData[4] = ct.MajorVersion;
                    sData[5] = ct.MinorVersion;
                    retData.Add(sData);
                    break;

                default:
                    break;
            }

            return (retData);
        }

        public static string GetClientTypeString(ClientCode dt)
        {
            string ret;
            switch (dt)
            {
                case ClientCode.CC_MAP_INFO:
                    ret = "MapInfo";
                    break;

                default:
                    ret = "";
                    break;
            }
            return (ret);
        }

    }
}
